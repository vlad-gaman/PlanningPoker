using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Service.Validations;

public class RoomExistsValidation(IRoomRepository rooms)
{
    public ServiceResponse Validate(string roomId)
    {
        return rooms.Get(roomId) is null
            ? ServiceResponse.Failure(ServiceError.RoomNotFound(roomId))
            : ServiceResponse.Success();
    }
}
