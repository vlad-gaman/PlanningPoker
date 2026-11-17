using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class ApiErrorMapper : IApiErrorMapper
{
    public ApiError Map(ServiceError error)
    {
        return new ApiError
        {
            Code = error.Code,
            Message = error.Message,
            Metadata = new Dictionary<string, string>(error.Metadata)
        };
    }
}
