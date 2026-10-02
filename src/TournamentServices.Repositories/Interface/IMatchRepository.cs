using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public interface IMatchRepository : IRepository<Match>
{
    Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId);
}
