using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class JoinRoomResultMapper : IJoinRoomResultMapper
{
    public JoinRoomResult Map(Room room, Person person, IReadOnlyList<Person> people, VoteStatus voteStatus, bool personAdded)
    {
        return new JoinRoomResult
        {
            Room = room,
            Person = person,
            People = people,
            VoteStatus = voteStatus,
            PersonAdded = personAdded,
            ResetCountdown = room.VotingEnabled && person.PersonType != PersonTypes.Observer
        };
    }
}
