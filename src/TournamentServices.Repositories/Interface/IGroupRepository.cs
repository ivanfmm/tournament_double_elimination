using TournamentServices.Domain;

namespace TournamentServices.Repositories;
public interface IGroupRepository : IRepository<Group>
{
    Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId);
    Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name);
}
