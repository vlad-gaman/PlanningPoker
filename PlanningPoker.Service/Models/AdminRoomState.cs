namespace PlanningPoker.Service.Models;

public enum AdminRoomState
{
    Voting,
    Revealed,

    /// <summary>Came back from the store after a restart and nobody has reconnected yet.</summary>
    Restored
}
