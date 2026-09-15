using TournamentServices.Domain;

namespace TournamentServices.Delegates;

// Decision 5 del equipo: los Delegates solo manejan objetos de Domain,
// nunca DTOs. El mapeo a DTO vive en Api/Extensions.
public interface IMatchDelegate
{
    Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId);

    // null si no existe. El 404 lo arma la capa Api.
    Task<Match?> GetByIdAsync(string id);

    // Lanza DomainValidationException (422) si algun equipo no pertenece
    // al torneo, si no estan en el mismo grupo, o si son el mismo equipo.
    Task<Match> CreateAsync(
        string tournamentId,
        string homeTeamId,
        string visitorTeamId,
        string? groupId = null);

    // Lanza NotFoundException si el match no existe y
    // DomainValidationException si algun marcador es negativo.
    Task<Match> UpdateScoreAsync(string matchId, int homeTeamScore, int visitorTeamScore);

    // Lanza NotFoundException si el match no existe.
    Task DeleteAsync(string id);
}
