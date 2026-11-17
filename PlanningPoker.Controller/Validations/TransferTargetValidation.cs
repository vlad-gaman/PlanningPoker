using PlanningPoker.Common.Models;

namespace PlanningPoker.Controller.Validations;

public class TransferTargetValidation
{
    /// <summary>Parses the target person guid; anything that is not a guid fails with <c>invalid_guid</c>.</summary>
    public ServiceResponse<Guid> Validate(string? targetPersonGuid)
    {
        return Guid.TryParse(targetPersonGuid, out var guid)
            ? ServiceResponse<Guid>.Success(guid)
            : ServiceResponse<Guid>.Failure(ServiceError.ValidationFailed("targetPersonGuid", "invalid_guid"));
    }
}
