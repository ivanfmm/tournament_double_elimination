using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class GroupDelegate : IGroupDelegate
{
    private readonly IGroupRepository _groupRepository;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITeamRepository _teamRepository;

    public GroupDelegate(IGroupRepository groupRepository, ITournamentRepository tournamentRepository, ITeamRepository teamRepository)
    {
        _groupRepository = groupRepository;
        _tournamentRepository = tournamentRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId)
    {
        await GetTournamentOrThrowAsync(tournamentId);
        return await _groupRepository.GetByTournamentAsync(tournamentId);
    }

    public async Task<Group?> GetByIdAsync(string tournamentId, string groupId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);

        // Un grupo de otro torneo se trata igual que uno inexistente.
        if (group is null || group.TournamentId != tournamentId)
        {
            return null;
        }

        return group;
    }

    public async Task<Group> CreateAsync(string tournamentId, string name)
    {
        var tournament = await GetTournamentOrThrowAsync(tournamentId);

        // El constructor valida el nombre vacio.
        var group = new Group(Guid.NewGuid().ToString(), tournamentId, name);

        // Tournament.AddGroup ya valida nombre repetido y maximo de grupos.
        // Se reusa la regla de Domain en vez de repetirla aqui.
        tournament.AddGroup(group);

        await _groupRepository.AddAsync(group);
        return group;
    }

    public async Task<Group> UpdateAsync(string tournamentId, string groupId, string name)
    {
        var group = await GetGroupOrThrowAsync(tournamentId, groupId);

        // Si solo cambian mayusculas del mismo nombre, no es duplicado.
        var isSameName = string.Equals(group.Name, name.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!isSameName && await _groupRepository.ExistsByNameInTournamentAsync(tournamentId, name))
        {
            throw new DomainValidationException($"Ya existe un grupo con el nombre '{name}' en este torneo.");
        }

        group.Name = name;
        await _groupRepository.UpdateAsync(group);
        return group;
    }

    public async Task DeleteAsync(string tournamentId, string groupId)
    {
        await GetGroupOrThrowAsync(tournamentId, groupId);

        // Los equipos quedan "desasignados" solos: la relacion vive
        // en la lista TeamIds del grupo, que se borra junto con el.
        await _groupRepository.DeleteAsync(groupId);
    }

    public async Task AssignTeamsAsync(string tournamentId, string groupId, IReadOnlyList<string> teamIds)
    {
        var tournament = await GetTournamentOrThrowAsync(tournamentId);
        var group = await GetGroupOrThrowAsync(tournamentId, groupId);

        if (teamIds.Count == 0)
        {
            throw new DomainValidationException("Debes enviar al menos un equipo.");
        }

        if (teamIds.Distinct().Count() != teamIds.Count)
        {
            throw new DomainValidationException("La lista de equipos tiene ids repetidos.");
        }

        foreach (var teamId in teamIds)
        {
            await EnsureTeamExistsAsync(teamId);
            EnsureTeamIsNotAssigned(tournament, teamId);
        }

        var totalTeams = group.TeamIds.Count + teamIds.Count;
        if (totalTeams > tournament.Format.MaxTeamsPerGroup)
        {
            throw new DomainValidationException(
                $"El grupo admite maximo {tournament.Format.MaxTeamsPerGroup} equipos.");
        }

        foreach (var teamId in teamIds)
        {
            group.AddTeam(teamId);
        }

        await _groupRepository.UpdateAsync(group);
    }

    private async Task<Tournament> GetTournamentOrThrowAsync(string tournamentId)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
        {
            throw new NotFoundException($"No existe un torneo con id '{tournamentId}'.");
        }

        return tournament;
    }

    private async Task<Group> GetGroupOrThrowAsync(string tournamentId, string groupId)
    {
        var group = await GetByIdAsync(tournamentId, groupId);
        if (group is null)
        {
            throw new NotFoundException($"No existe el grupo '{groupId}' en el torneo '{tournamentId}'.");
        }

        return group;
    }

    private async Task EnsureTeamExistsAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team is null)
        {
            throw new DomainValidationException($"El equipo '{teamId}' no existe.");
        }
    }

    // Team no guarda su torneo: un equipo "esta en el torneo" si aparece
    // en algun grupo del torneo (mismo criterio que MatchDelegate).
    private static void EnsureTeamIsNotAssigned(Tournament tournament, string teamId)
    {
        if (tournament.Groups.Any(g => g.TeamIds.Contains(teamId)))
        {
            throw new DomainValidationException($"El equipo '{teamId}' ya esta asignado a un grupo de este torneo.");
        }
    }
}
