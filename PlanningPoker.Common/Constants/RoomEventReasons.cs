namespace PlanningPoker.Common.Constants;

/// <summary>Why a room or person went away when it was not the ordinary way. Ordinary disposals and exits carry no reason.</summary>
public static class RoomEventReasons
{
    /// <summary>The Operator closed the room.</summary>
    public const string ClosedByOperator = "closedByOperator";

    /// <summary>The Operator removed the person.</summary>
    public const string RemovedByOperator = "removedByOperator";
}
