using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Validations;

public class RoomIdentifierValidation
{
    public ServiceResponse Validate(string? roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            return Invalid("required");
        }

        if (roomId.Length > RoomConstraints.IdMaxLength)
        {
            return Invalid("too_long");
        }

        if (!roomId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            return Invalid("invalid_characters");
        }

        return ServiceResponse.Success();
    }

    private static ServiceResponse Invalid(string reason)
    {
        return ServiceResponse.Failure(ServiceError.Create(ErrorCodes.ValidationFailed, "The field 'roomId' is invalid.", ("field", "roomId"), ("reason", reason)));
    }
}
