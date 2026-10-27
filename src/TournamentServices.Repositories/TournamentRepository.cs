using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
namespace TournamentServices.Repositories;

public class TournamentRepository : ITournamentRepository
{
    private readonly TournamentDbContext _context;
    private readonly IGroupRepository _groupRepository;

    public TournamentRepository(TournamentDbContext context,  IGroupRepository groupRepository)
    {
        _context = context;
        _groupRepository = groupRepository;
    }

    public async Task<Tournament?> GetByIdAsync(string id)
    {
        return await _context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tournament?> GetByIdWithDetailsAsync(string id)
    {
        var tournament = await GetByIdAsync(id);
        if (tournament is null) return null;

        tournament.Groups.AddRange(await _groupRepository.GetByTournamentAsync(id));

        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == id)
            .ToListAsync();
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

    // Soft delete, igual que equipos, grupos y partidos. Antes era un borrado
    // real y chocaba con la llave foranea de los grupos ya marcados con
    // deleted_at. Los grupos y partidos los marca TournamentDelegate.
    public async Task<bool> DeleteAsync(string id)
    {
        int rowsAffected = await _context.Tournaments
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => EF.Property<DateTime?>(t, "deleted_at"), DateTime.UtcNow));

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Tournaments
            .AnyAsync(t => t.Name.ToLower() == normalized);
    }
}