using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

public class AdminLoginRequestValidation
{
    public const int MaxLength = 256;

    public ServiceResponse Validate(AdminLoginRequest request)
    {
        var errors = new List<ServiceError>();
        Check(errors, "username", request.Username);
        Check(errors, "password", request.Password);
        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }

    private static void Check(List<ServiceError> errors, string field, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            errors.Add(ServiceError.ValidationFailed(field, "required"));
        }
        else if (value.Length > MaxLength)
        {
            errors.Add(ServiceError.ValidationFailed(field, "too_long"));
        }
    }
}
