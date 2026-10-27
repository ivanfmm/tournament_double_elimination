using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly TournamentDbContext _context;

    public MatchRepository(TournamentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId)
    {
        return await _context.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<Match?> GetByIdAsync(string id)
    {
        return await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Match> AddAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task UpdateAsync(Match match)
    {
        _context.Matches.Update(match);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        int rowsAffected = await _context.Matches
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => EF.Property<DateTime?>(m, "deleted_at"), DateTime.UtcNow));

        return rowsAffected > 0;
    }
}
