using TournamentServices.Domain;

namespace TournamentServices.Repositories;

// CONTRATO PROVISIONAL -- pertenece a la Persona A (Team).
// Se declara aqui solo para poder mockearlo en MatchDelegateTests.
// Los metodos son exactamente los del plan de trabajo del equipo.
// Cuando la Persona A suba su version, este archivo se borra.
public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id);

    Task<IReadOnlyList<Team>> GetAllAsync();

    Task AddAsync(Team team);

    Task UpdateAsync(Team team);

    Task DeleteAsync(string id);

    Task<bool> ExistsByNameAsync(string name);
}
