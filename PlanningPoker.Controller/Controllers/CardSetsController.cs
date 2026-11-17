using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

[ApiController]
[Route("api/card-sets")]
[Produces("application/json")]
public class CardSetsController(
    ICardSetService cardSets,
    ICardSetResponseMapper cardSetResponseMapper,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>Lists the card sets a room can use.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<List<CardSetResponse>>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return ToResult(cardSets.GetAll(), options => options.Select(cardSetResponseMapper.Map).ToList());
    }
}
