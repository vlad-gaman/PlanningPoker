using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class CardResponseMapper : ICardResponseMapper
{
    public CardResponse Map(CardOption card)
    {
        return new CardResponse { Value = card.Value, Display = card.Display };
    }
}
