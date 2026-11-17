namespace PlanningPoker.Service.Services;

/// <summary>How the Operator's actions reach the room clients; the Controller layer implements it so the Service layer never references SignalR.</summary>
public interface IRoomAdminNotifier
{
    /// <summary>Tells the room it was closed by the Operator, then takes the given connections out of the room's group.</summary>
    Task RoomClosedAsync(string roomId, IReadOnlyList<string> connectionIds);
}
