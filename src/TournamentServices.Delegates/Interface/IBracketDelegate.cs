using TournamentServices.Domain;

namespace TournamentServices.Delegates;

public interface IBracketDelegate
{
    Task<Tournament> StartAsync(string tournamentId);
    Task<Match> ReportScoreAsync(string tournamentId, string matchId, int homeTeamScore, int visitorTeamScore);
    Task<Team> GetChampionAsync(string tournamentId);
}
