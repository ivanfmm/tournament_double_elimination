using System.Threading.Tasks;
using TournamentServices.Domain;

namespace TournamentServices.Delegates
{
    public interface IGroupDelegate
    {
        Task AddAsync(Group group);
    }
}