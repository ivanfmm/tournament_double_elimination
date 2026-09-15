using TournamentServices.Domain;

namespace TournamentServices.Delegates;

// Decision 5 del equipo: solo objetos de Domain, nunca DTOs.
public interface IGroupDelegate
{
    // Lanza NotFoundException si el torneo no existe.
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);

    // null si el grupo no existe o pertenece a otro torneo.
    Task<Group?> GetByIdAsync(string tournamentId, string groupId);

    // NotFoundException si el torneo no existe.
    // DomainValidationException si el nombre se repite o ya no caben grupos.
    Task<Group> CreateAsync(string tournamentId, string name);

    // NotFoundException si el grupo no existe.
    // DomainValidationException si el nombre ya lo usa otro grupo del torneo.
    Task<Group> UpdateAsync(string tournamentId, string groupId, string name);

    // NotFoundException si el grupo no existe.
    Task DeleteAsync(string tournamentId, string groupId);

    // Agrega equipos al grupo (no reemplaza los que ya tenia).
    // NotFoundException si el torneo o el grupo no existen.
    // DomainValidationException si un equipo no existe, viene repetido,
    // ya esta en algun grupo del torneo, o se excede maxTeamsPerGroup.
    Task AssignTeamsAsync(string tournamentId, string groupId, IReadOnlyList<string> teamIds);
}
