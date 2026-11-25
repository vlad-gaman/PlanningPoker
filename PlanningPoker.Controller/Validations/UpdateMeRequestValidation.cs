using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

public class UpdateMeRequestValidation
{
    public ServiceResponse Validate(UpdateMeRequest request)
    {
        var errors = new List<ServiceError>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            errors.Add(ServiceError.ValidationFailed("name", "required"));
        }
        else if (name.Length < PersonConstraints.NameMinLength)
        {
            errors.Add(ServiceError.ValidationFailed("name", "too_short"));
        }
        else if (name.Length > PersonConstraints.NameMaxLength)
        {
            errors.Add(ServiceError.ValidationFailed("name", "too_long"));
        }

        if (!PersonTypes.IsValid(request.PersonType))
        {
            errors.Add(ServiceError.ValidationFailed("personType", "invalid_person_type"));
        }

        if (request.CardBack is not null && !CardBacks.IsValid(request.CardBack))
        {
            errors.Add(ServiceError.ValidationFailed("cardBack", "invalid_card_back"));
        }

        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }
}
