using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class TournamentDelegate(ITournamentRepository tournamentRepository, IGroupRepository groupRepository, IMatchRepository matchRepository) : ITournamentDelegate
{
    public async Task<Tournament> GetByIdAsync(string id)
        => await tournamentRepository.GetByIdWithDetailsAsync(id)
           ?? throw new NotFoundException($"Torneo '{id}' no encontrado.");

    public Task<IReadOnlyList<Tournament>> GetAllAsync()
        => tournamentRepository.GetAllAsync();

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        EnsureNameAndFormat(tournament);

        if (await tournamentRepository.ExistsByNameAsync(tournament.Name))
        {
            throw new DuplicateNameException($"Ya existe un torneo con el nombre '{tournament.Name}'.");
        }

        tournament.Id = Guid.NewGuid().ToString();

        tournament.Groups.Clear();
        tournament.Matches.Clear();

        return await tournamentRepository.AddAsync(tournament);
    }

    public async Task<Tournament> UpdateAsync(Tournament tournament)
    {
        var existing = await GetByIdAsync(tournament.Id);

        EnsureNameAndFormat(tournament);
        EnsureFormatFitsExistingGroups(existing, tournament.Format);

        if (existing.Name != tournament.Name && await tournamentRepository.ExistsByNameAsync(tournament.Name))
        {
            throw new DuplicateNameException($"Ya existe un torneo con el nombre '{tournament.Name}'.");
        }

        existing.Name = tournament.Name;
        existing.Format = tournament.Format;

        await tournamentRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<Tournament> PatchAsync(string id, TournamentPatch patch)
    {
        var existing = await GetByIdAsync(id);

        var newName = patch.Name is null ? existing.Name : new Tournament { Name = patch.Name }.Name;

        var newFormat = existing.Format;
        if (patch.HasFormatChanges)
        {
            newFormat = new TournamentFormat(patch.MaxTeamsPerGroup ?? existing.Format.MaxTeamsPerGroup, patch.NumberOfGroups ?? existing.Format.NumberOfGroups, patch.Type ?? existing.Format.Type);
            EnsureFormatFitsExistingGroups(existing, newFormat);
        }

        existing.Name = newName;
        existing.Format = newFormat;

        await tournamentRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task DeleteAsync(string id)
    {
        await GetByIdAsync(id);

        var groups = await groupRepository.GetByTournamentAsync(id);
        foreach (var group in groups)
        {
            await groupRepository.DeleteAsync(group.Id);
        }

        var matches = await matchRepository.GetByTournamentAsync(id);
        foreach (var match in matches)
        {
            await matchRepository.DeleteAsync(match.Id);
        }

        var deleted = await tournamentRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"Torneo '{id}' no encontrado.");
        }
    }

    private static void EnsureNameAndFormat(Tournament tournament)
    {
        if (string.IsNullOrWhiteSpace(tournament.Name))
            throw new DomainValidationException("El nombre del torneo no puede estar vacío.");

        if (tournament.Format is null)
            throw new DomainValidationException("El formato del torneo es requerido.");
    }

    private static void EnsureFormatFitsExistingGroups(Tournament existing, TournamentFormat format)
    {
        if (existing.Groups.Count > format.NumberOfGroups)
        {
            throw new BusinessRuleException($"El torneo ya tiene {existing.Groups.Count} grupos; no se puede reducir a {format.NumberOfGroups}.");
        }

        var biggest = existing.Groups.Count == 0 ? 0 : existing.Groups.Max(g => g.TeamIds.Count);
        if (biggest > format.MaxTeamsPerGroup)
        {
             throw new BusinessRuleException($"Hay un grupo con {biggest} equipos; no se puede reducir el máximo a {format.MaxTeamsPerGroup}.");
        }
    }
}