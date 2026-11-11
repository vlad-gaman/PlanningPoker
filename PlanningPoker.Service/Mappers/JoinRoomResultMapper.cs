using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class JoinRoomResultMapper : IJoinRoomResultMapper
{
    public JoinRoomResult Map(Room room, Person person, IReadOnlyList<Person> people, VoteStatus voteStatus, IReadOnlyList<CardOption> cards, bool personAdded, bool wasOffgrid, bool resetCountdown, string? myMark)
    {
        return new JoinRoomResult
        {
            Room = room,
            Person = person,
            People = people,
            VoteStatus = voteStatus,
            Cards = cards,
            PersonAdded = personAdded,
            MyMark = myMark,
            WasOffgrid = wasOffgrid,
            ResetCountdown = resetCountdown
        };
    }
}
