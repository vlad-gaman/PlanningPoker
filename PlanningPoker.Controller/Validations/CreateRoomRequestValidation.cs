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
            return ServiceResponse.Failure(ServiceError.ValidationFailed("configuration", "required"));
        }

        var errors = new List<ServiceError>();
        if (!CardSets.IsValid(configuration.CardSet))
        {
            errors.Add(ServiceError.ValidationFailed("configuration.cardSet", "unknown_card_set"));
        }

        if (configuration.CountdownSeconds is < RoomConstraints.CountdownMinSeconds or > RoomConstraints.CountdownMaxSeconds)
        {
            errors.Add(ServiceError.ValidationFailed("configuration.countdownSeconds", "out_of_range"));
        }

        if (configuration.MaxParticipants < RoomConstraints.MaxParticipantsMin)
        {
            errors.Add(ServiceError.ValidationFailed("configuration.maxParticipants", "too_small"));
        }

        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }
}
