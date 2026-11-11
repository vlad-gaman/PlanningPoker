using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Hubs;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Services;

public class RoomEventPublisher(
    IHubContext<RoomHub> hub,
    IPersonJoinedEventMapper personJoinedEventMapper,
    IRoomCountdownEventMapper roomCountdownEventMapper,
    IPersonExitedEventMapper personExitedEventMapper,
    IPersonOffgridEventMapper personOffgridEventMapper,
    IPersonBackEventMapper personBackEventMapper,
    IOwnershipTransferredEventMapper ownershipTransferredEventMapper,
    IRoomDisposedEventMapper roomDisposedEventMapper,
    IVoteCastEventMapper voteCastEventMapper,
    IVoteStatusResponseMapper voteStatusResponseMapper,
    IPersonTypeChangedEventMapper personTypeChangedEventMapper,
    IRoomConfigurationUpdatedEventMapper roomConfigurationUpdatedEventMapper,
    IVotesClearedEventMapper votesClearedEventMapper) : IRoomEventPublisher, IRoomCountdownNotifier, IRoomPresenceNotifier
{
    public async Task PublishPersonJoinedAsync(JoinRoomResult result, string joinerConnectionId)
    {
        var others = hub.Clients.GroupExcept(result.Room.Id, [joinerConnectionId]);

        // The person is not new to the room (another tab or a reconnect): from everyone else's view nothing changed, unless
        // they had been announced offgrid. Then they hear the person is back, and a voter who had not voted holds up the round again.
        if (!result.PersonAdded)
        {
            if (result.WasOffgrid)
            {
                await others.SendAsync(HubEvents.PersonBack, personBackEventMapper.Map(result));
                if (result.ResetCountdown)
                {
                    await others.SendAsync(HubEvents.CountdownReset, roomCountdownEventMapper.Map(result.Room.Configuration.CountdownSeconds, reset: true));
                }
            }

            return;
        }

        await others.SendAsync(HubEvents.PersonJoined, personJoinedEventMapper.Map(result));
        if (result.ResetCountdown)
        {
            await others.SendAsync(HubEvents.CountdownReset, roomCountdownEventMapper.Map(result.Room.Configuration.CountdownSeconds, reset: true));
        }
    }

    Task IRoomPresenceNotifier.PersonOffgridAsync(PersonOffgridResult result)
    {
        return hub.Clients.Group(result.RoomId).SendAsync(HubEvents.PersonOffgrid, personOffgridEventMapper.Map(result));
    }

    Task IRoomPresenceNotifier.PersonRemovedAsync(ExitRoomResult result)
    {
        return PublishPersonExitedAsync(result);
    }

    public async Task PublishPersonExitedAsync(ExitRoomResult result)
    {
        // The person still has another connection to the room: from everyone else's view nothing changed.
        if (!result.PersonLeft)
        {
            return;
        }

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

    public async Task PublishVoteCastAsync(VoteResult result)
    {
        var group = hub.Clients.Group(result.RoomId);
        await group.SendAsync(HubEvents.VoteCast, voteCastEventMapper.Map(result.PersonGuid));
        await group.SendAsync(HubEvents.VoteStatusUpdate, voteStatusResponseMapper.Map(result.VoteStatus));
    }

    public Task PublishPersonTypeChangedAsync(PersonTypeChangeResult result)
    {
        return result.Changed
            ? hub.Clients.Group(result.RoomId).SendAsync(HubEvents.PersonTypeChanged, personTypeChangedEventMapper.Map(result))
            : Task.CompletedTask;
    }

    public async Task PublishRoomConfigurationUpdatedAsync(RoomConfigurationChangeResult result)
    {
        var group = hub.Clients.Group(result.RoomId);
        await group.SendAsync(HubEvents.RoomConfigurationUpdated, roomConfigurationUpdatedEventMapper.Map(result));
        if (result.VotesCleared)
        {
            await group.SendAsync(HubEvents.VotesCleared, votesClearedEventMapper.Map(result.VoteStatus.RoundStartedAt));
        }
    }

    public Task PublishOwnershipTransferredAsync(OwnershipTransferResult result)
    {
        return result.NewOwner is { } newOwner
            ? hub.Clients.Group(result.RoomId).SendAsync(HubEvents.OwnershipTransferred, ownershipTransferredEventMapper.Map(newOwner))
            : Task.CompletedTask;
    }

    public Task PublishVotesClearedAsync(ClearVotesResult result)
    {
        return hub.Clients.Group(result.RoomId).SendAsync(HubEvents.VotesCleared, votesClearedEventMapper.Map(result.RoundStartedAt));
    }

    public Task PublishVotesShownAsync(string roomId, VoteStatus status)
    {
        return hub.Clients.Group(roomId).SendAsync(HubEvents.VotesShown, voteStatusResponseMapper.Map(status));
    }

    public Task PublishCountdownUpdateAsync(string roomId, int countdown)
    {
        return hub.Clients.Group(roomId).SendAsync(HubEvents.CountdownUpdate, roomCountdownEventMapper.Map(countdown, reset: false));
    }

    Task IRoomCountdownNotifier.CountdownTickedAsync(string roomId, int remaining)
    {
        return PublishCountdownUpdateAsync(roomId, remaining);
    }

    Task IRoomCountdownNotifier.VotesRevealedAsync(string roomId, VoteStatus status)
    {
        return PublishVotesShownAsync(roomId, status);
    }
}
