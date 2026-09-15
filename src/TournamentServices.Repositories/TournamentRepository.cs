using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
namespace TournamentServices.Repositories;

public class TournamentRepository : ITournamentRepository
{
    private readonly TournamentDbContext _context;

    public TournamentRepository(TournamentDbContext context)
    {
        _context = context;
    }

    public async Task<Tournament?> GetByIdAsync(string id)
    {
        var tournament = await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament is null)
        {
            return null;
        }

        var groups = await _context.Groups
            .AsNoTracking()
            .Where(g => g.TournamentId == id)
            .ToListAsync();

        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == id)
            .ToListAsync();

        tournament.Groups.AddRange(groups);
        tournament.Matches.AddRange(matches);

        return tournament;
    }

    public async Task<IReadOnlyList<Tournament>> GetAllAsync()
    {
        return await _context.Tournaments
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Tournament> AddAsync(Tournament tournament)
    {
        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync();
        return tournament;
    }

    public async Task UpdateAsync(Tournament tournament)
    {
        _context.Tournaments.Update(tournament);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var tournament = await _context.Tournaments.FindAsync(id);

        if (tournament is null)
        {
            return false;
        }

        var groups = await _context.Groups.Where(g => g.TournamentId == id).ToListAsync();
        var matches = await _context.Matches.Where(m => m.TournamentId == id).ToListAsync();

        _context.Groups.RemoveRange(groups);
        _context.Matches.RemoveRange(matches);
        _context.Tournaments.Remove(tournament);

        await _context.SaveChangesAsync();
        return true;
    }
}