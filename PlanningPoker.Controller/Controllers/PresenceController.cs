using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

[ApiController]
[Route("api/presence")]
[Produces("application/json")]
public class PresenceController(
    IPresenceService presence,
    ClosingRequestValidation validation,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>
    /// Sent by a page that is closing (the browser's beacon, so the answer is never read). The caller's room shows them offgrid
    /// at once and removes them shortly after, unless they join again first, which a reload does.
    /// </summary>
    [HttpPost("closing")]
    [EnableRateLimiting(RateLimitingConstants.PresenceClosingPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public IActionResult Closing([FromBody] ClosingRequest request)
    {
        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return Failure(validated);
        }

        var session = RequireSessionPerson();
        if (!session.IsSuccess)
        {
            return Failure(session);
        }

        var result = presence.Closing(session.Data!.Guid, request.ConnectionId);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    private ObjectResult Failure(ServiceResponse result)
    {
        return StatusCode(apiResponseMapper.GetStatusCode(result), apiResponseMapper.Map(result));
    }
}
