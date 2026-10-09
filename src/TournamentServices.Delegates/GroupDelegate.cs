using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class GroupDelegate(IGroupRepository groupRepository,ITournamentRepository tournamentRepository, ITeamRepository teamRepository) : IGroupDelegate
{
    public Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId)
        => groupRepository.GetByTournamentAsync(tournamentId);

    public async Task<Group?> GetByIdAsync(string tournamentId, string groupId)
    {
        var group = await groupRepository.GetByIdAsync(groupId);

        return group is not null && group.TournamentId == tournamentId ? group : null;
    }

        public async Task<Group> CreateAsync(string tournamentId, string name)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Torneo '{tournamentId}' no encontrado.");
 
        var group = new Group(Guid.NewGuid().ToString(), tournamentId, name);
 
        if (await groupRepository.ExistsByNameInTournamentAsync(tournamentId, group.Name))
        {
            throw new BusinessRuleException($"Ya existe un grupo '{group.Name}' en este torneo.");
        }

        var currentGroups = await groupRepository.GetByTournamentAsync(tournamentId);
        if (currentGroups.Count >= tournament.Format.NumberOfGroups)
        {
            throw new BusinessRuleException($"El torneo ya alcanzó el máximo de {tournament.Format.NumberOfGroups} grupos.");
 
        }
        return await groupRepository.AddAsync(group);
    }

    public async Task<Group> UpdateAsync(string tournamentId, string groupId, string name)
    {
        var group = await GetExistingGroupAsync(tournamentId, groupId);

        var newName = new Group { Name = name }.Name;

        var nameChanged = !string.Equals(group.Name, newName, StringComparison.OrdinalIgnoreCase);
        if (nameChanged && await groupRepository.ExistsByNameInTournamentAsync(tournamentId, newName))
        {
            throw new BusinessRuleException($"Ya existe un grupo '{newName}' en este torneo.");
        }

        group.Name = newName;
        await groupRepository.UpdateAsync(group);
        return group;
    }

    public async Task DeleteAsync(string tournamentId, string groupId)
    {
        await GetExistingGroupAsync(tournamentId, groupId);

        var deleted = await groupRepository.DeleteAsync(groupId);
        if (!deleted)
        {
            throw new NotFoundException($"Grupo '{groupId}' no encontrado.");
        }
    }

    public async Task AssignTeamsAsync(string tournamentId, string groupId, IReadOnlyList<string> teamIds)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Torneo '{tournamentId}' no encontrado.");

        var group = await GetExistingGroupAsync(tournamentId, groupId);

        if (teamIds.Count != teamIds.Distinct().Count())
        {
            throw new BusinessRuleException("La lista de equipos contiene IDs duplicados.");            
        }

        foreach (var teamId in teamIds)
        {
            if (await teamRepository.GetByIdAsync(teamId) is null)
            {
                throw new BusinessRuleException($"El equipo '{teamId}' no existe.");   
            }
        }

        var tournamentGroups = await groupRepository.GetByTournamentAsync(tournamentId);
        foreach (var teamId in teamIds)
        {
            if (tournamentGroups.Any(g => g.TeamIds.Contains(teamId)))
            {
                throw new BusinessRuleException($"El equipo '{teamId}' ya está asignado a un grupo de este torneo.");   
            }
        }

        var max = tournament.Format.MaxTeamsPerGroup;
        if (group.TeamIds.Count + teamIds.Count > max)
        {
            throw new BusinessRuleException($"No se pueden asignar los equipos. El grupo '{group.Name}' ya tiene {group.TeamIds.Count} equipos y el máximo permitido es {max}.");   
        }

        foreach (var teamId in teamIds)
        {
            group.AddTeam(teamId);
        }

        await groupRepository.UpdateAsync(group);
    }

    private async Task<Group> GetExistingGroupAsync(string tournamentId, string groupId)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group is null || group.TournamentId != tournamentId)
        {
            throw new NotFoundException($"Grupo '{groupId}' no encontrado en el torneo '{tournamentId}'.");   
        }
        return group;
    }
}