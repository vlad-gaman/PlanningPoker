using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface ICardResponseMapper
{
    CardResponse Map(CardOption card);
}
