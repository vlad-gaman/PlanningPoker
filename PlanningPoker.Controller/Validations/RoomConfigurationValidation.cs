using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

/// <summary>The shape rules of a room configuration, shared by room creation and the owner's settings.</summary>
public class RoomConfigurationValidation
{
    /// <param name="configuration">The configuration to check; null fails as required.</param>
    /// <param name="fieldPrefix">Put before each field name in the errors, for example <c>configuration.</c> inside a larger request.</param>
    public ServiceResponse Validate(RoomConfigurationRequest? configuration, string fieldPrefix = "")
    {
        if (configuration is null)
        {
            return ServiceResponse.Failure(ServiceError.ValidationFailed(fieldPrefix.Length == 0 ? "configuration" : fieldPrefix.TrimEnd('.'), "required"));
        }

        var errors = new List<ServiceError>();
        if (!CardSets.IsValid(configuration.CardSet))
        {
            errors.Add(ServiceError.ValidationFailed($"{fieldPrefix}cardSet", "unknown_card_set"));
        }

        if (configuration.CountdownSeconds is < RoomConstraints.CountdownMinSeconds or > RoomConstraints.CountdownMaxSeconds)
        {
            errors.Add(ServiceError.ValidationFailed($"{fieldPrefix}countdownSeconds", "out_of_range"));
        }

        if (configuration.MaxParticipants < RoomConstraints.MaxParticipantsMin)
        {
            errors.Add(ServiceError.ValidationFailed($"{fieldPrefix}maxParticipants", "too_small"));
        }

        return errors.Count == 0 ? ServiceResponse.Success() : ServiceResponse.Failure([.. errors]);
    }
}
