using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class TeamDelegate(ITeamRepository teamRepository) : ITeamDelegate
{
    public Task<Team?> GetByIdAsync(string id)
        => teamRepository.GetByIdAsync(id);

    public Task<List<Team>> GetAllAsync()
        => teamRepository.GetAllAsync();

    public async Task<Team> CreateAsync(Team team)
    {
        if (string.IsNullOrWhiteSpace(team.Name))
        {
            throw new DomainValidationException("El nombre del equipo no puede estar vacío.");
        }
        if (await teamRepository.ExistsByNameAsync(team.Name))
        {
            throw new DuplicateNameException($"Ya existe un equipo con el nombre '{team.Name}'.");
        }
        team.Id = Guid.NewGuid().ToString();
        return await teamRepository.AddAsync(team);
    }

    public async Task<Team> UpdateAsync(string id, Team team)
    {
        if (string.IsNullOrWhiteSpace(team.Name))
        {
            throw new DomainValidationException("El nombre del equipo no puede estar vacío.");
        }
        var existing = await teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Equipo '{id}' no encontrado.");

        var nameChanged = !string.Equals(existing.Name, team.Name, StringComparison.OrdinalIgnoreCase);
        if (nameChanged && await teamRepository.ExistsByNameAsync(team.Name))
        {
            throw new DuplicateNameException($"Ya existe un equipo con el nombre '{team.Name}'.");
        }
        existing.Name = team.Name;
        await teamRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await teamRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"Equipo '{id}' no encontrado.");
        }
    }
}