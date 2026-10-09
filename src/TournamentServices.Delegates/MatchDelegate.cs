using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

public class MatchDelegate(IMatchRepository matchRepository, ITournamentRepository tournamentRepository, IGroupRepository groupRepository) : IMatchDelegate
{
    public Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId)
        => matchRepository.GetByTournamentAsync(tournamentId);

    public Task<Match?> GetByIdAsync(string id)
        => matchRepository.GetByIdAsync(id);

    public async Task<Match> CreateAsync(string tournamentId, string homeTeamId, string visitorTeamId, string? groupId = null)
    {
        if (await tournamentRepository.GetByIdAsync(tournamentId) is null)
            throw new NotFoundException($"Torneo '{tournamentId}' no encontrado.");

        if (string.Equals(homeTeamId, visitorTeamId, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("El equipo local y el visitante no pueden ser el mismo.");
        }
        var groups = await groupRepository.GetByTournamentAsync(tournamentId);

        if (groupId is not null)
        {
            var group = groups.FirstOrDefault(g => g.Id == groupId) ?? throw new BusinessRuleException($"El grupo '{groupId}' no pertenece al torneo.");

            EnsureTeamIn(group.TeamIds, homeTeamId, "local", "al grupo");
            EnsureTeamIn(group.TeamIds, visitorTeamId, "visitante", "al grupo");
        }
        else
        {
            var tournamentTeamIds = groups.SelectMany(g => g.TeamIds).ToHashSet();
            EnsureTeamIn(tournamentTeamIds, homeTeamId, "local", "al torneo");
            EnsureTeamIn(tournamentTeamIds, visitorTeamId, "visitante", "al torneo");
        }

        var match = new Match(Guid.NewGuid().ToString(), tournamentId, homeTeamId, visitorTeamId, groupId);
        return await matchRepository.AddAsync(match);
    }

    public async Task<Match> UpdateScoreAsync(string matchId, int homeTeamScore, int visitorTeamScore)
    {
        var match = await matchRepository.GetByIdAsync(matchId) ?? throw new NotFoundException($"Partido '{matchId}' no encontrado.");

        match.SetScore(new Score(homeTeamScore, visitorTeamScore));

        await matchRepository.UpdateAsync(match);
        return match;
    }

    public async Task DeleteAsync(string id)
    {
        var match = await matchRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Partido '{id}' no encontrado.");
        var deleted = await matchRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"Partido '{id}' no encontrado.");
        }
    }

    private static void EnsureTeamIn(IEnumerable<string> teamIds, string teamId, string role, string scope)
    {
        if (!teamIds.Contains(teamId))
        {
            throw new BusinessRuleException($"El equipo {role} '{teamId}' no pertenece {scope}.");
        }
    }
}