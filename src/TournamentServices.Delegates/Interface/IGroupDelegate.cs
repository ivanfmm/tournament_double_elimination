using TournamentServices.Domain;
namespace TournamentServices.Delegates;

public interface IGroupDelegate
{
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);
    Task<Group?> GetByIdAsync(string tournamentId, string groupId);
    Task<Group> CreateAsync(string tournamentId, string name);
    Task<Group> UpdateAsync(string tournamentId, string groupId, string name);
    Task DeleteAsync(string tournamentId, string groupId);
    Task AssignTeamsAsync(string tournamentId, string groupId, IReadOnlyList<string> teamIds);
}
