using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

public class ClosingRequestValidation
{
    public ServiceResponse Validate(ClosingRequest? request)
    {
        return string.IsNullOrWhiteSpace(request?.ConnectionId)
            ? ServiceResponse.Failure(ServiceError.ValidationFailed("connectionId", "required"))
            : ServiceResponse.Success();
    }
}
