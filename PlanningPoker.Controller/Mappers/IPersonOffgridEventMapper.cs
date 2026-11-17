using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IPersonOffgridEventMapper
{
    PersonOffgridEvent Map(PersonOffgridResult result);
}
