using TournamentServices.Domain;
namespace TournamentServices.Delegates;

public interface ITournamentDelegate
{
    Task<Tournament> GetByIdAsync(string id);
    Task<IReadOnlyList<Tournament>> GetAllAsync();
    Task<Tournament> CreateAsync(Tournament tournament);
    Task<Tournament> UpdateAsync(Tournament tournament);
    Task<Tournament> PatchAsync(string id, TournamentPatch patch);
    Task DeleteAsync(string id);
}