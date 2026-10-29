using PlanningPoker.Controller.Models;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Mappers;

public interface ICardSetResponseMapper
{
    CardSetResponse Map(CardSetOption option);
}
