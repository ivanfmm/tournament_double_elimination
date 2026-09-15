using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class TeamDelegate : ITeamDelegate
{
    private readonly ITeamRepository _teamRepository;

    public TeamDelegate(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<Team?> GetByIdAsync(string id)
    {
        return await _teamRepository.GetByIdAsync(id);
    }

    public async Task<List<Team>> GetAllAsync()
    {
        return await _teamRepository.GetAllAsync();
    }

    public async Task<Team> CreateAsync(Team team)
    {
        var nameExists = await _teamRepository.ExistsByNameAsync(team.Name);
        if (nameExists)
        {
            throw new DomainValidationException($"Ya existe un equipo con el nombre '{team.Name}'.");
        }

        await _teamRepository.AddAsync(team);
        return team;
    }

    public async Task<Team> UpdateAsync(string id, Team team)
    {
        var existing = await _teamRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new NotFoundException($"No se encontró un equipo con id '{id}'.");
        }

        var nameExists = await _teamRepository.ExistsByNameAsync(team.Name);
        if (nameExists && existing.Name != team.Name)
        {
            throw new DomainValidationException($"Ya existe un equipo con el nombre '{team.Name}'.");
        }

        existing.Name = team.Name;
        await _teamRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task DeleteAsync(string id)
    {
        var existing = await _teamRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new NotFoundException($"No se encontró un equipo con id '{id}'.");
        }

        await _teamRepository.DeleteAsync(id);
    }
}
