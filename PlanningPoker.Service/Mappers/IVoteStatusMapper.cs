using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IVoteStatusMapper
{
    VoteStatus Map(RoomEntity room, IReadOnlyCollection<Person> members);
}
