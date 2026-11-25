using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class CardBackChangedEventMapper : ICardBackChangedEventMapper
{
    public CardBackChangedEvent Map(CardBackChangeResult result)
    {
        return new CardBackChangedEvent { PersonGuid = result.PersonGuid, CardBack = result.CardBack };
    }
}
