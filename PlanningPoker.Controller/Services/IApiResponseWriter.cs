using Microsoft.AspNetCore.Http;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Services;

public interface IApiResponseWriter
{
    Task WriteAsync(HttpContext context, ServiceResponse response);
}
