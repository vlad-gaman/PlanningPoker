using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IApiErrorMapper
{
    ApiError Map(ServiceError error);
}
