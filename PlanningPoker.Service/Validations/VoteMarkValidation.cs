using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Service.Validations;

/// <summary>A vote must be a card of the room's set. The shape rules are the old hub's input checks.</summary>
public class VoteMarkValidation(ICardSetService cardSets)
{
    private static readonly char[] ForbiddenCharacters = ['<', '>', '"', '\'', '&', '\0', '\r', '\n'];

    public ServiceResponse Validate(string cardSet, string? mark)
    {
        if (string.IsNullOrEmpty(mark))
        {
            return Invalid("required");
        }

        if (mark.Length > RoomConstraints.VoteMarkMaxLength)
        {
            return Invalid("too_long");
        }

        if (mark.IndexOfAny(ForbiddenCharacters) >= 0)
        {
            return Invalid("invalid_characters");
        }

        // "hide" is what the wire shows for a masked vote, so it can never be a real one.
        if (mark == RoomConstraints.HiddenVote)
        {
            return Invalid("reserved");
        }

        var cards = cardSets.GetCards(cardSet);
        return cards.IsSuccess && cards.Data!.Any(card => card.Value == mark) ? ServiceResponse.Success() : Invalid("unknown_card");
    }

    private static ServiceResponse Invalid(string reason)
    {
        return ServiceResponse.Failure(ServiceError.ValidationFailed("mark", reason));
    }
}
