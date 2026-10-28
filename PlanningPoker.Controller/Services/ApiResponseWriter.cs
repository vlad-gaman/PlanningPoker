using Microsoft.AspNetCore.Http;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;

namespace PlanningPoker.Controller.Services;

public class ApiResponseWriter(IApiResponseMapper mapper) : IApiResponseWriter
{
    public Task WriteAsync(HttpContext context, ServiceResponse response)
    {
        context.Response.StatusCode = mapper.GetStatusCode(response);
        return context.Response.WriteAsJsonAsync(mapper.Map(response));
    }
}
