using TournamentServices.Domain;
namespace TournamentServices.Repositories;

public interface ITournamentRepository : IRepository<Tournament>
{
    Task<IReadOnlyList<Tournament>> GetAllAsync();
}