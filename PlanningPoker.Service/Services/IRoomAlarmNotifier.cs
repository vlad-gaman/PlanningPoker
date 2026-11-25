using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>How the Alarm timer reaches the room clients; the Controller layer implements it so the Service layer never references SignalR.</summary>
public interface IRoomAlarmNotifier
{
    Task AlarmChangedAsync(AlarmChangeResult result);
}
