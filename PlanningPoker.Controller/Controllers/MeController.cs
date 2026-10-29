using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

[ApiController]
[Route("api/me")]
[Produces("application/json")]
public class MeController(
    IPersonService persons,
    ISessionCookieService cookies,
    UpdateMeRequestValidation validation,
    IPersonResponseMapper personResponseMapper,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>Returns the person tied to the session cookie.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PersonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        return ToResult(persons.Get(cookies.Read(Request)), personResponseMapper.Map);
    }

    /// <summary>Sets the name and person type; mints the session cookie when no valid person exists.</summary>
    [HttpPut]
    [ProducesResponseType<ApiResponse<PersonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status415UnsupportedMediaType)]
    public IActionResult Put([FromBody] UpdateMeRequest request)
    {
        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return ToResult(ServiceResponse<Person>.Failure(validated), personResponseMapper.Map);
        }

        var existingGuid = cookies.Read(Request);
        var result = persons.Upsert(existingGuid, request.Name, request.PersonType);
        if (result.IsSuccess && result.Data!.Guid != existingGuid)
        {
            cookies.Write(Response, result.Data.Guid);
        }

        return ToResult(result, personResponseMapper.Map);
    }
}
