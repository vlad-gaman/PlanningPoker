using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Hubs;

public class RoomHub(
    IRoomService rooms,
    IJoinRoomResponseMapper joinRoomResponseMapper,
    IPersonJoinedEventMapper personJoinedEventMapper,
    IPersonExitedEventMapper personExitedEventMapper,
    IOwnershipTransferredEventMapper ownershipTransferredEventMapper,
    IRoomDisposedEventMapper roomDisposedEventMapper,
    ICountdownResetEventMapper countdownResetEventMapper,
    IApiResponseMapper apiResponseMapper,
    ILogger<RoomHub> logger) : Hub
{
    /// <summary>Adds the caller to the room and returns the people in it.</summary>
    public async Task<ApiResponse<JoinRoomResponse>> JoinRoom(string roomId)
    {
        var person = Context.GetHttpContext()?.GetSessionPerson();
        var joined = person is null
            ? ServiceResponse<JoinRoomResult>.Failure(ServiceError.Create(ErrorCodes.NoSession, "There is no valid session."))
            : rooms.Join(person.Guid, Context.ConnectionId, roomId);
        if (!joined.IsSuccess)
        {
            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(joined));
        }

        var result = joined.Data!;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync(HubEvents.PersonJoined, personJoinedEventMapper.Map(result));
        if (result.ResetCountdown)
        {
            await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync(HubEvents.CountdownReset, countdownResetEventMapper.Map(result.Room));
        }

        return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Success(joinRoomResponseMapper.Map(result)));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var person = Context.GetHttpContext()?.GetSessionPerson();
        logger.LogInformation(exception, "Hub disconnected: person {PersonGuid}", person?.Guid);
        if (person is not null)
        {
            var exited = rooms.Exit(person.Guid, Context.ConnectionId);
            if (exited.IsSuccess)
            {
                var result = exited.Data!;
                await Clients.Group(result.RoomId).SendAsync(HubEvents.PersonExited, personExitedEventMapper.Map(result));
                if (result.NewOwner is not null)
                {
                    await Clients.Group(result.RoomId).SendAsync(HubEvents.OwnershipTransferred, ownershipTransferredEventMapper.Map(result.NewOwner));
                }

                if (result.RoomDisposed)
                {
                    await Clients.Group(result.RoomId).SendAsync(HubEvents.RoomDisposed, roomDisposedEventMapper.Map(result.RoomId));
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
