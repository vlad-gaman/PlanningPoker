using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IPersonResponseMapper
{
    PersonResponse Map(Person person);
}
