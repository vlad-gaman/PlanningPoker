using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface ICountdownResetEventMapper
{
    CountdownResetEvent Map(Room room);
}
