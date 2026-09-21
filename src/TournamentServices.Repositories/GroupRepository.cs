using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly TournamentDbContext _context;

    public GroupRepository(TournamentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Group>> GetByTournamentAsync(string tournamentId)
    {
        return await _context.Groups
            .AsNoTracking()
            .Where(g => g.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<Group?> GetByIdAsync(string id)
    {
        return await _context.Groups.FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddAsync(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null)
        {
            return;
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Groups
            .AnyAsync(g => g.TournamentId == tournamentId && g.Name.ToLower() == normalized);
    }
}
