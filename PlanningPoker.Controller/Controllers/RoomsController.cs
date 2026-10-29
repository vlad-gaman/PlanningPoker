using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public class RoomsController(
    IRoomService rooms,
    CreateRoomRequestValidation validation,
    IRoomSettingsMapper roomSettingsMapper,
    IRoomCreatedResponseMapper roomCreatedResponseMapper,
    IRoomExistsResponseMapper roomExistsResponseMapper,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>Creates a room owned by the session person.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<RoomCreatedResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status415UnsupportedMediaType)]
    public IActionResult Create([FromBody] CreateRoomRequest request)
    {
        var session = RequireSessionPerson();
        if (!session.IsSuccess)
        {
            return ToResult(ServiceResponse<Room>.Failure(session), roomCreatedResponseMapper.Map);
        }

        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return ToResult(ServiceResponse<Room>.Failure(validated), roomCreatedResponseMapper.Map);
        }

        return ToResult(rooms.Create(session.Data!.Guid, roomSettingsMapper.Map(request)), roomCreatedResponseMapper.Map, StatusCodes.Status201Created);
    }

    /// <summary>Tells whether a room exists.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<ApiResponse<RoomExistsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id)
    {
        return ToResult(rooms.Exists(id), roomExistsResponseMapper.Map);
    }
}
