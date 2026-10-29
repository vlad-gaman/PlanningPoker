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
            errors.Add(Invalid("name", "required"));
        }
        else if (name.Length < PersonConstraints.NameMinLength)
        {
            errors.Add(Invalid("name", "too_short"));
        }
        else if (name.Length > PersonConstraints.NameMaxLength)
        {
            errors.Add(Invalid("name", "too_long"));
        }

        if (!PersonTypes.IsValid(request.PersonType))
        {
            errors.Add(Invalid("personType", "invalid_person_type"));
        }

        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }

    private static ServiceError Invalid(string field, string reason)
    {
        return ServiceError.Create(ErrorCodes.ValidationFailed, $"The field '{field}' is invalid.", ("field", field), ("reason", reason));
    }
}
