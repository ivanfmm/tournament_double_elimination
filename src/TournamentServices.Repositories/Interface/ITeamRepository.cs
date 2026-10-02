using TournamentServices.Domain;

namespace TournamentServices.Repositories;


public interface ITeamRepository : IRepository<Team>
{
    Task<List<Team>> GetAllAsync();
    Task<bool> ExistsByNameAsync(string name);
}
