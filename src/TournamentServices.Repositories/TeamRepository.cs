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
        return await _context.Teams.ToListAsync();
    }

    public async Task AddAsync(Team team)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Team team)
    {
        _context.Teams.Update(team);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team is not null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Teams.AnyAsync(t => t.Name == name);
    }
}
