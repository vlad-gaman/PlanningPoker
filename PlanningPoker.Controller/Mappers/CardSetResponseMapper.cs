using PlanningPoker.Controller.Models;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Mappers;

public class CardSetResponseMapper : ICardSetResponseMapper
{
    public CardSetResponse Map(CardSetOption option)
    {
        return new CardSetResponse { Value = option.Value, Display = option.Display };
    }
}
