using TournamentServices.Domain;

namespace TournamentServices.Delegates;

public interface IMatchDelegate
{
    Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId);
    Task<Match?> GetByIdAsync(string id);
    Task<Match> CreateAsync(string tournamentId, string homeTeamId, string visitorTeamId, string? groupId = null);
    Task<Match> UpdateScoreAsync(string matchId, int homeTeamScore, int visitorTeamScore);
    Task DeleteAsync(string id);
}
