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
        [ErrorCodes.NotFound] = StatusCodes.Status404NotFound
    };

    public const int Fallback = StatusCodes.Status500InternalServerError;
}
