using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

public class CardSetService : ICardSetService
{
    public ServiceResponse<IReadOnlyList<CardSetOption>> GetAll()
    {
        return ServiceResponse<IReadOnlyList<CardSetOption>>.Success(CardSets.All);
    }

    public ServiceResponse<IReadOnlyList<CardOption>> GetCards(string cardSet)
    {
        return CardSets.Cards.TryGetValue(cardSet, out var cards)
            ? ServiceResponse<IReadOnlyList<CardOption>>.Success(cards)
            : ServiceResponse<IReadOnlyList<CardOption>>.Failure(ServiceError.ValidationFailed("cardSet", "unknown_card_set"));
    }
}
