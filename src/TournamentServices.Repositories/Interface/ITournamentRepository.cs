using TournamentServices.Domain;
namespace TournamentServices.Repositories;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(string id);
    Task<IReadOnlyList<Tournament>> GetAllAsync();
    Task<Tournament> AddAsync(Tournament tournament);
    Task UpdateAsync(Tournament tournament);
    Task<bool> DeleteAsync(string id);
}