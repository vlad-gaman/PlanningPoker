using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Hubs;

public class RoomHub(
    IRoomService rooms,
    IRoomEventPublisher roomEvents,
    IJoinRoomResponseMapper joinRoomResponseMapper,
    IApiResponseMapper apiResponseMapper,
    ILogger<RoomHub> logger) : Hub
{
    /// <summary>Adds the caller to the room and returns the people in it.</summary>
    public async Task<ApiResponse<JoinRoomResponse>> JoinRoom(string roomId)
    {
        var person = Context.GetHttpContext()?.GetSessionPerson();
        var joined = person is null
            ? ServiceResponse<JoinRoomResult>.Failure(ServiceError.NoSession())
            : rooms.Join(person.Guid, Context.ConnectionId, roomId);
        if (!joined.IsSuccess)
        {
            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(joined));
        }

        var result = joined.Data!;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        if (Context.ConnectionAborted.IsCancellationRequested)
        {
            // The client left while joining: undo the membership instead of announcing a person who is already gone.
            var exited = rooms.Exit(result.Person.Guid, Context.ConnectionId);
            if (exited.IsSuccess)
            {
                await roomEvents.PublishPersonExitedAsync(exited.Data!);
            }

            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(
                ServiceError.Create(ErrorCodes.ConnectionClosed, "The connection closed before the join completed.")));
        }

        await roomEvents.PublishPersonJoinedAsync(result, Context.ConnectionId);
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
                await roomEvents.PublishPersonExitedAsync(exited.Data!);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
