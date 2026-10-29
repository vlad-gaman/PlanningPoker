using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PlanningPoker.Controller.Extensions;

namespace PlanningPoker.Controller.Hubs;

public class RoomHub(ILogger<RoomHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Hub connected: person {PersonGuid}", Context.GetHttpContext()?.GetSessionPerson()?.Guid);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(exception, "Hub disconnected: person {PersonGuid}", Context.GetHttpContext()?.GetSessionPerson()?.Guid);
        await base.OnDisconnectedAsync(exception);
    }
}
