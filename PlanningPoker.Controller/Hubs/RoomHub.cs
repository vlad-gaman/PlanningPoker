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
using PlanningPoker.Service.Validations;

namespace PlanningPoker.Controller.Hubs;

/// <summary>
/// Hub workflow only: the service decides, the publisher broadcasts. Errors come back as ApiResponse failures; the
/// hub-only codes are <c>not_in_room</c> (exit of an unknown connection, never sent to clients by JoinRoom) and
/// <c>connection_closed</c> (the client disconnected during JoinRoom; the join was rolled back and nothing was announced).
/// </summary>
public class RoomHub(
    IRoomService rooms,
    IRoomEventPublisher roomEvents,
    RoomIdentifierValidation identifierValidation,
    IJoinRoomResponseMapper joinRoomResponseMapper,
    IApiResponseMapper apiResponseMapper,
    ILogger<RoomHub> logger) : Hub
{
    /// <summary>Adds the caller to the room and returns the people in it.</summary>
    public async Task<ApiResponse<JoinRoomResponse>> JoinRoom(string roomId)
    {
        var person = Context.GetHttpContext()?.GetSessionPerson();
        if (person is null)
        {
            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(ServiceError.NoSession()));
        }

        // An id that is not a valid group name is refused before any group is touched.
        var identifier = identifierValidation.Validate(roomId);
        if (!identifier.IsSuccess)
        {
            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(identifier));
        }

        // Subscribe to the group before the membership is visible, so no broadcast can fall between the snapshot and
        // the subscription. A client already in room A that calls JoinRoom("B") is briefly in group B; the client does
        // not check room ids on PersonJoined or PersonExited, and the group is dropped again if the join is refused.
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var joined = rooms.Join(person.Guid, Context.ConnectionId, roomId);
        if (!joined.IsSuccess)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            return apiResponseMapper.Map(ServiceResponse<JoinRoomResponse>.Failure(joined));
        }

        var result = joined.Data!;
        if (Context.ConnectionAborted.IsCancellationRequested)
        {
            // The client left while joining: undo the membership instead of announcing a person who is already gone.
            await ExitAndPublishAsync(person.Guid);
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
            await ExitAndPublishAsync(person.Guid);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task ExitAndPublishAsync(Guid personGuid)
    {
        var exited = rooms.Exit(personGuid, Context.ConnectionId);
        if (exited.IsSuccess)
        {
            await roomEvents.PublishPersonExitedAsync(exited.Data!);
        }
    }
}
