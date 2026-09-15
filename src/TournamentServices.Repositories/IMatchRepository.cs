using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public interface IMatchRepository
{
    // Match no tiene propiedades de navegacion hacia Team, asi que aqui
    // no hay Include: solo devuelve los partidos con sus ids de equipo.
    Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId);

    // Devuelve null si no existe. Quien decide el 404 es el Delegate.
    Task<Match?> GetByIdAsync(string id);

    Task AddAsync(Match match);

    Task UpdateAsync(Match match);

    // Idempotente: si el id no existe no hace nada ni lanza.
    Task DeleteAsync(string id);
}
