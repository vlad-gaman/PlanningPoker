using Microsoft.AspNetCore.Http;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IApiResponseMapper
{
    ApiResponse<T> Map<T>(ServiceResponse<T> response);

    ApiResponse<object> Map(ServiceResponse response);

    int GetStatusCode(ServiceResponse response, int successStatusCode = StatusCodes.Status200OK);
}
