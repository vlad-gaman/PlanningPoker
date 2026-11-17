using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IExitRoomResultMapper
{
    ExitRoomResult Map(string roomId, Person person, bool personLeft, bool roomDisposed, Person? newOwner, VotingFollowUp followUp, string? reason = null, IReadOnlyList<string>? connectionIds = null);
}
