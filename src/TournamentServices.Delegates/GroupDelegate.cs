using System.Threading.Tasks;
using TournamentServices.Domain;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates
{
    public class GroupDelegate : IGroupDelegate
    {
        
        private readonly IGroupRepository _groupRepository;

        public GroupDelegate(IGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }
        public async Task AddAsync(Group group)
        {
            await _groupRepository.AddAsync(group);
        }
    }
}