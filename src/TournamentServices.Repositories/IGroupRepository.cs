using TournamentServices.Domain;

namespace TournamentServices.Repositories;

// Contrato unico de Group. Vive en Repositories (no en Domain) para
// respetar la direccion de dependencias Api -> Delegates -> Repositories -> Domain.
public interface IGroupRepository
{
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);

    // Devuelve null si no existe. Quien decide el 404 es el Delegate.
    Task<Group?> GetByIdAsync(string id);

    Task AddAsync(Group group);

    Task UpdateAsync(Group group);

    // Idempotente: si el id no existe no hace nada ni lanza.
    Task DeleteAsync(string id);

    // Compara sin distinguir mayusculas, igual que Tournament.AddGroup.
    Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name);
}
