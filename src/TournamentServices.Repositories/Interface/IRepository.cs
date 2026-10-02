using TournamentServices.Domain;

namespace TournamentServices.Repositories;
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(string id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task<bool> DeleteAsync(string id);
} 