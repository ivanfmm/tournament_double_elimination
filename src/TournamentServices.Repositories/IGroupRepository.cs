using TournamentServices.Domain;

namespace TournamentServices.Repositories;

// CONTRATO PROVISIONAL -- pertenece a la Persona B (Group).
// Se declara aqui solo para poder mockearlo en MatchDelegateTests.
// Los metodos son exactamente los del plan de trabajo del equipo:
// no se invento ninguno para no romper el acuerdo previo.
// Cuando la Persona B suba su version, este archivo se borra.
public interface IGroupRepository
{
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);

    Task<Group?> GetByIdAsync(string id);

    Task AddAsync(Group group);

    Task UpdateAsync(Group group);

    Task DeleteAsync(string id);

    Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name);
}
