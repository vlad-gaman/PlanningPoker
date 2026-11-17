using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>How the presence sweep reaches the room clients; the Controller layer implements it so the Service layer never references SignalR.</summary>
public interface IRoomPresenceNotifier
{
    /// <summary>Tells the room that a person is offgrid now: still a member with their vote, but no longer waited for.</summary>
    Task PersonOffgridAsync(PersonOffgridResult result);

    /// <summary>Tells the room that a person stayed offgrid for the whole removal window and is removed.</summary>
    Task PersonRemovedAsync(ExitRoomResult result);
}
