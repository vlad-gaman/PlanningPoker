using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Controllers;

public abstract class ApiControllerBase(IApiResponseMapper apiResponseMapper) : ControllerBase
{
    /// <summary>Maps a service response to an ApiResponse body with the status code its first error (or success) calls for.</summary>
    protected ObjectResult ToResult<TSource, TResponse>(ServiceResponse<TSource> result, Func<TSource, TResponse> map, int successStatusCode = StatusCodes.Status200OK)
    {
        var body = apiResponseMapper.Map(new ServiceResponse<TResponse>
        {
            Data = result.IsSuccess ? map(result.Data!) : default,
            Errors = result.Errors
        });
        return StatusCode(apiResponseMapper.GetStatusCode(result, successStatusCode), body);
    }

    /// <summary>The session person for this request, or a <c>no_session</c> failure when there is none.</summary>
    protected ServiceResponse<Person> RequireSessionPerson()
    {
        var person = HttpContext.GetSessionPerson();
        return person is null
            ? ServiceResponse<Person>.Failure(ServiceError.NoSession())
            : ServiceResponse<Person>.Success(person);
    }
}
