using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Hubs;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Services;

public class RoomEventPublisher(
    IHubContext<RoomHub> hub,
    IPersonJoinedEventMapper personJoinedEventMapper,
    ICountdownResetEventMapper countdownResetEventMapper,
    IPersonExitedEventMapper personExitedEventMapper,
    IOwnershipTransferredEventMapper ownershipTransferredEventMapper,
    IRoomDisposedEventMapper roomDisposedEventMapper) : IRoomEventPublisher
{
    public async Task PublishPersonJoinedAsync(JoinRoomResult result, string joinerConnectionId)
    {
        var others = hub.Clients.GroupExcept(result.Room.Id, [joinerConnectionId]);
        await others.SendAsync(HubEvents.PersonJoined, personJoinedEventMapper.Map(result));
        if (result.ResetCountdown)
        {
            await others.SendAsync(HubEvents.CountdownReset, countdownResetEventMapper.Map(result.Room));
        }
    }

    public async Task PublishPersonExitedAsync(ExitRoomResult result)
    {
        var group = hub.Clients.Group(result.RoomId);
        await group.SendAsync(HubEvents.PersonExited, personExitedEventMapper.Map(result));
        if (result.NewOwner is not null)
        {
            await group.SendAsync(HubEvents.OwnershipTransferred, ownershipTransferredEventMapper.Map(result.NewOwner));
        }

        // Mostly a hook: the last member's connection has already left the group, so no client receives this broadcast.
        if (result.RoomDisposed)
        {
            await group.SendAsync(HubEvents.RoomDisposed, roomDisposedEventMapper.Map(result.RoomId));
        }
    }
}
