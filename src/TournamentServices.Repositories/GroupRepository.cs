using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
using TournamentServices.Domain.Repositories;

namespace TournamentServices.Repositories
{
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
                .Where(g => g.TournamentId == tournamentId)
                .ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(string id)
        {
            return await _context.Groups.FindAsync(id);
        }

        public async Task AddAsync(Group group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Group group)
        {
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var group = await GetByIdAsync(id);
            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByNameInTournamentAsync(string name, string tournamentId)
        {
            return await _context.Groups
                .AnyAsync(g => g.Name == name && g.TournamentId == tournamentId);
        }
    }
}