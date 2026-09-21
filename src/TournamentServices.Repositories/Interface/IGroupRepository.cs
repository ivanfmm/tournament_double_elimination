using TournamentServices.Domain;

namespace TournamentServices.Repositories;
public interface IGroupRepository
{
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);
    Task<Group?> GetByIdAsync(string id);
    Task AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(string id);
    Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name);
}
