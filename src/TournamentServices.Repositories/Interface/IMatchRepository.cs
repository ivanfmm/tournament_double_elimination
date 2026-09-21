using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public interface IMatchRepository
{
    Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId);
    Task<Match?> GetByIdAsync(string id);
    Task AddAsync(Match match);
    Task UpdateAsync(Match match);
    Task DeleteAsync(string id);
}
