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

    // Aqui SI se trackea: el Delegate carga el match, le aplica SetScore
    // y luego llama UpdateAsync sobre esa misma instancia.
    public async Task<Match?> GetByIdAsync(string id)
    {
        return await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Match match)
    {
        _context.Matches.Update(match);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match is null)
        {
            return;
        }

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();
    }
}
