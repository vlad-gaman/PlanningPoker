using Microsoft.AspNetCore.Http;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Constants;

public static class ErrorStatusCodes
{
    public static readonly IReadOnlyDictionary<string, int> Table = new Dictionary<string, int>
    {
        [ErrorCodes.InternalError] = StatusCodes.Status500InternalServerError,
        [ErrorCodes.ValidationFailed] = StatusCodes.Status400BadRequest,
        [ErrorCodes.UnsupportedMediaType] = StatusCodes.Status415UnsupportedMediaType,
        [ErrorCodes.NoSession] = StatusCodes.Status401Unauthorized,
        [ErrorCodes.NotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.RoomNotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.RoomIdGenerationFailed] = StatusCodes.Status409Conflict,
        [ErrorCodes.NotRoomOwner] = StatusCodes.Status403Forbidden
    };

    public const int Fallback = StatusCodes.Status500InternalServerError;
}
