using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly TournamentDbContext _context;

    public TeamRepository(TournamentDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> GetByIdAsync(string id)
    {
        return await _context.Teams.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Team>> GetAllAsync()
    {
        return await _context.Teams
        .AsNoTracking()
        .Where(t => EF.Property<DateTime?>(t, "deleted_at") == null)
        .ToListAsync();
    }

    public async Task<Team> AddAsync(Team team)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        return team;
    }

    public async Task UpdateAsync(Team team)
    {
        _context.Teams.Update(team);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        int rowsAffected = await _context.Teams
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => EF.Property<DateTime?>(t, "deleted_at"), DateTime.UtcNow));

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Teams
            .Where(t => EF.Property<DateTime?>(t, "deleted_at") == null)
            .AnyAsync(t => t.Name == name);
    }
}
