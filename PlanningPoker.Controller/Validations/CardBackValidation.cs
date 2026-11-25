using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Validations;

public class CardBackValidation
{
    public ServiceResponse Validate(string? cardBack)
    {
        return CardBacks.IsValid(cardBack)
            ? ServiceResponse.Success()
            : ServiceResponse.Failure(ServiceError.ValidationFailed("cardBack", "invalid_card_back"));
    }
}
