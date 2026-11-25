using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanningPoker.Common.Constants;
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

    /// <summary>Sets the name, person type and, when given, card back; mints the session cookie when no valid person exists.</summary>
    [HttpPut]
    [EnableRateLimiting(RateLimitingConstants.MeUpdatePolicy)]
    [ProducesResponseType<ApiResponse<PersonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public IActionResult Put([FromBody] UpdateMeRequest request)
    {
        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return ToResult(ServiceResponse<PersonSession>.Failure(validated), MapPerson);
        }

        var existingToken = cookies.Read(Request);
        var result = persons.Upsert(existingToken, request.Name, request.PersonType, request.CardBack);
        if (result.IsSuccess && result.Data!.SessionToken != existingToken)
        {
            cookies.Write(Response, result.Data.SessionToken);
        }

        return ToResult(result, MapPerson);
    }

    /// <summary>Only the person goes into the body; the session token travels in the cookie alone.</summary>
    private PersonResponse MapPerson(PersonSession session)
    {
        return personResponseMapper.Map(session.Person);
    }
}
