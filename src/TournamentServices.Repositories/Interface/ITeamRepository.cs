using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id);
    Task<List<Team>> GetAllAsync();
    Task AddAsync(Team team);
    Task UpdateAsync(Team team);
    Task DeleteAsync(string id);
    Task<bool> ExistsByNameAsync(string name);
}
