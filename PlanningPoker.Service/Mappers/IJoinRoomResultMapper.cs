using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IJoinRoomResultMapper
{
    JoinRoomResult Map(Room room, Person person, IReadOnlyList<Person> people, VoteStatus voteStatus);
}
