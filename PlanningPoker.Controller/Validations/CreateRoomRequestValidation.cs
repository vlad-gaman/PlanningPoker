using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

public class CreateRoomRequestValidation(RoomConfigurationValidation configurationValidation)
{
    public ServiceResponse Validate(CreateRoomRequest request)
    {
        return configurationValidation.Validate(request.Configuration, "configuration.");
    }
}
