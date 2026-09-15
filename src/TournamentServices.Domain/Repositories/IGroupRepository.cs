using System.Threading.Tasks;

namespace TournamentServices.Domain.Repositories
{
    public interface IGroupRepository
    {
        Task<Group?> GetByTournamentAsync(string tournamentId);
        Task<Group?> GetByIdAsync(string id);
        Task AddAsync(Group group);
        Task UpdateAsync(Group group);
        Task DeleteAsync(string id);
        Task<bool> ExistsByNameInTournamentAsync(string name, string tournamentId);
    }
}