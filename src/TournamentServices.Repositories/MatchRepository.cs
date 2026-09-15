using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly TournamentDbContext _db;

    public MatchRepository(TournamentDbContext db)
    {
        _db = db;
    }

    // AsNoTracking porque es una lectura de solo consulta: nadie va a
    // modificar estas entidades, y sin tracking la consulta es mas barata.
    public async Task<IReadOnlyList<Match>> GetByTournamentAsync(string tournamentId)
    {
        return await _db.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();
    }

    // Aqui SI se trackea: el Delegate carga el match, le aplica SetScore
    // y luego llama UpdateAsync sobre esa misma instancia.
    public async Task<Match?> GetByIdAsync(string id)
    {
        return await _db.Matches.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddAsync(Match match)
    {
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Match match)
    {
        _db.Matches.Update(match);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match is null)
        {
            return;
        }

        _db.Matches.Remove(match);
        await _db.SaveChangesAsync();
    }
}
