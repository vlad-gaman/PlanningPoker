using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

public class CreateRoomRequestValidation
{
    public ServiceResponse Validate(CreateRoomRequest request)
    {
        var configuration = request.Configuration;
        if (configuration is null)
        {
            return ServiceResponse.Failure(Invalid("configuration", "required"));
        }

        var errors = new List<ServiceError>();
        if (!CardSets.IsValid(configuration.CardSet))
        {
            errors.Add(Invalid("configuration.cardSet", "unknown_card_set"));
        }

        if (configuration.CountdownSeconds is < RoomConstraints.CountdownMinSeconds or > RoomConstraints.CountdownMaxSeconds)
        {
            errors.Add(Invalid("configuration.countdownSeconds", "out_of_range"));
        }

        if (configuration.MaxParticipants < RoomConstraints.MaxParticipantsMin)
        {
            errors.Add(Invalid("configuration.maxParticipants", "too_small"));
        }

        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }

    private static ServiceError Invalid(string field, string reason)
    {
        return ServiceError.Create(ErrorCodes.ValidationFailed, $"The field '{field}' is invalid.", ("field", field), ("reason", reason));
    }
}
