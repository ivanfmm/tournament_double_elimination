using TournamentServices.Domain;

namespace TournamentServices.Delegates;

public interface ITeamDelegate
{
    Task<Team?> GetByIdAsync(string id);
    Task<List<Team>> GetAllAsync();
    Task<Team> CreateAsync(Team team);
    Task<Team> UpdateAsync(string id, Team team);
    Task DeleteAsync(string id);
}
