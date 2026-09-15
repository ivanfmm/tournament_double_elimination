using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class MatchDelegate : IMatchDelegate
{
    private readonly IMatchRepository _matchRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IGroupRepository _groupRepository;

    public MatchDelegate(
        IMatchRepository matchRepository,
        ITeamRepository teamRepository,
        IGroupRepository groupRepository)
    {
        _matchRepository = matchRepository;
        _teamRepository = teamRepository;
        _groupRepository = groupRepository;
    }

    public Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId)
    {
        return _matchRepository.GetByTournamentAsync(tournamentId);
    }

    public Task<Match?> GetByIdAsync(string id)
    {
        return _matchRepository.GetByIdAsync(id);
    }

    public async Task<Match> CreateAsync(
        string tournamentId,
        string homeTeamId,
        string visitorTeamId,
        string? groupId = null)
    {
        await EnsureTeamExistsAsync(homeTeamId);
        await EnsureTeamExistsAsync(visitorTeamId);

        // Team no tiene TournamentId, asi que la pertenencia al torneo
        // se deduce de los grupos: un equipo esta en el torneo si aparece
        // en el TeamIds de alguno de sus grupos.
        var groups = await _groupRepository.GetByTournamentAsync(tournamentId);
        EnsureTeamIsInTournament(groups, homeTeamId);
        EnsureTeamIsInTournament(groups, visitorTeamId);

        if (groupId is not null)
        {
            EnsureTeamsShareGroup(groups, groupId, homeTeamId, visitorTeamId);
        }

        // El constructor de Match valida que home y visitor sean distintos
        // y que ninguno venga vacio. No se repite esa logica aqui.
        var match = new Match(
            Guid.NewGuid().ToString(),
            tournamentId,
            homeTeamId,
            visitorTeamId,
            groupId);

        await _matchRepository.AddAsync(match);
        return match;
    }

    public async Task<Match> UpdateScoreAsync(string matchId, int homeTeamScore, int visitorTeamScore)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match is null)
        {
            throw new NotFoundException($"No existe el partido '{matchId}'.");
        }

        // El constructor de Score rechaza negativos y SetScore calcula
        // Winner e IsCompleted. Esa logica vive en Domain, no aqui.
        match.SetScore(new Score(homeTeamScore, visitorTeamScore));

        await _matchRepository.UpdateAsync(match);
        return match;
    }

    public async Task DeleteAsync(string id)
    {
        var match = await _matchRepository.GetByIdAsync(id);
        if (match is null)
        {
            throw new NotFoundException($"No existe el partido '{id}'.");
        }

        await _matchRepository.DeleteAsync(id);
    }

    private async Task EnsureTeamExistsAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team is null)
        {
            throw new DomainValidationException($"El equipo '{teamId}' no existe.");
        }
    }

    private static void EnsureTeamIsInTournament(IReadOnlyList<Group> groups, string teamId)
    {
        if (!groups.Any(g => g.TeamIds.Contains(teamId)))
        {
            throw new DomainValidationException($"El equipo '{teamId}' no pertenece a este torneo.");
        }
    }

    private static void EnsureTeamsShareGroup(
        IReadOnlyList<Group> groups,
        string groupId,
        string homeTeamId,
        string visitorTeamId)
    {
        var group = groups.FirstOrDefault(g => g.Id == groupId);
        if (group is null)
        {
            throw new DomainValidationException($"El grupo '{groupId}' no pertenece a este torneo.");
        }

        if (!group.TeamIds.Contains(homeTeamId) || !group.TeamIds.Contains(visitorTeamId))
        {
            throw new DomainValidationException("Ambos equipos deben pertenecer al grupo indicado.");
        }
    }
}
