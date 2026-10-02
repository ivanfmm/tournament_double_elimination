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
        var groups = await _context.Groups
            .AsNoTracking()
            .Where(g => g.TournamentId == tournamentId)
            .ToListAsync();

        await LoadTeamIdsAsync(groups);
        return groups;
    }

    public async Task<Group?> GetByIdAsync(string id)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return null;

        await LoadTeamIdsAsync(new[] { group });
        return group;

    }

    public async Task<Group> AddAsync(Group group)
    {
        _context.Groups.Add(group);

        _context.GroupTeams.AddRange(
            group.TeamIds.Select(teamId => new GroupTeam { GroupId = group.Id, TeamId = teamId }));

        await _context.SaveChangesAsync();
        return group;
    }

    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);

        var current = await _context.GroupTeams
            .Where(gt => gt.GroupId == group.Id)
            .ToListAsync();

        var desired = group.TeamIds.ToHashSet();

        _context.GroupTeams.RemoveRange(current.Where(c => !desired.Contains(c.TeamId)));

        var existing = current.Select(c => c.TeamId).ToHashSet();
        _context.GroupTeams.AddRange(
            desired.Where(id => !existing.Contains(id))
                   .Select(id => new GroupTeam { GroupId = group.Id, TeamId = id }));

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
            int rowsAffected = await _context.Groups
            .Where(g => g.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(g => EF.Property<DateTime?>(g, "deleted_at"), DateTime.UtcNow));

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsByNameInTournamentAsync(string tournamentId, string name)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Groups
            .AnyAsync(g => g.TournamentId == tournamentId && g.Name.ToLower() == normalized);
    }

    private async Task LoadTeamIdsAsync(IReadOnlyCollection<Group> groups)
    {
        if (groups.Count == 0) return;

        var groupIds = groups.Select(g => g.Id).ToList();

        var teamsByGroup = (await _context.GroupTeams
                .AsNoTracking()
                .Where(gt => groupIds.Contains(gt.GroupId))
                .ToListAsync())
            .ToLookup(gt => gt.GroupId, gt => gt.TeamId);

        foreach (var g in groups)
            foreach (var teamId in teamsByGroup[g.Id])
                g.AddTeam(teamId);
    }
}
