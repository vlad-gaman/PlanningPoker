using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IJoinRoomResultMapper
{
    JoinRoomResult Map(Room room, Person person, IReadOnlyList<Person> people, VoteStatus voteStatus, IReadOnlyList<CardOption> cards, bool personAdded);
}
