using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class ExitRoomResultMapper : IExitRoomResultMapper
{
    public ExitRoomResult Map(string roomId, Person person, bool personLeft, bool roomDisposed, Person? newOwner, VotingFollowUp followUp)
    {
        return new ExitRoomResult { RoomId = roomId, Person = person, PersonLeft = personLeft, RoomDisposed = roomDisposed, NewOwner = newOwner, FollowUp = followUp };
    }
}
