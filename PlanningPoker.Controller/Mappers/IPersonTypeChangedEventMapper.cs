using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IPersonTypeChangedEventMapper
{
    PersonTypeChangedEvent Map(PersonTypeChangeResult result);
}
