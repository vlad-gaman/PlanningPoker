using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

/// <summary>What the management page polls: stats, live rooms with their people, and recent issues. Operator session required.</summary>
[ApiController]
[Route("api/admin/overview")]
[Produces("application/json")]
[Authorize(Policy = OperatorConstants.PolicyName)]
public class AdminOverviewController(
    IAdminOverviewService overview,
    IAdminOverviewResponseMapper mapper,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>The overview. Never contains vote data, not even whether someone has voted.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<AdminOverviewResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        return ToResult(overview.GetOverview(), mapper.Map);
    }
}
