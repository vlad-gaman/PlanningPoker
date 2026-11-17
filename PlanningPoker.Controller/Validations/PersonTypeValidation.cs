using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Validations;

public class PersonTypeValidation
{
    public ServiceResponse Validate(string? personType)
    {
        return PersonTypes.IsValid(personType)
            ? ServiceResponse.Success()
            : ServiceResponse.Failure(ServiceError.ValidationFailed("personType", "invalid_person_type"));
    }
}
