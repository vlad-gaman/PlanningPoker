using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Filters;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

/// <summary>What the Operator can do to live rooms. Needs the Operator session and, on every action, the anti-forgery token (send a JSON body, <c>{}</c> will do).</summary>
[ApiController]
[Route("api/admin/rooms")]
[Produces("application/json")]
[Authorize(Policy = OperatorConstants.PolicyName)]
[ValidateAdminAntiforgery]
public class AdminRoomsController(IAdminActionService actions, IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>Closes the room and deletes it from the store. Everyone in it is told it was closed by the Operator.</summary>
    [HttpPost("{roomId}/close")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(string roomId)
    {
        var result = await actions.CloseRoomAsync(roomId);
        return StatusCode(apiResponseMapper.GetStatusCode(result), apiResponseMapper.Map(result));
    }

    /// <summary>Removes a member (online or offgrid) from the room. Their vote is discarded; the room is told it was the Operator.</summary>
    [HttpPost("{roomId}/people/{personId:guid}/remove")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePerson(string roomId, Guid personId)
    {
        var result = await actions.RemovePersonAsync(roomId, personId);
        return StatusCode(apiResponseMapper.GetStatusCode(result), apiResponseMapper.Map(result));
    }
}
