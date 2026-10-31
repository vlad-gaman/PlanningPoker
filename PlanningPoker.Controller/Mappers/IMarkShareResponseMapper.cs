using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IMarkShareResponseMapper
{
    MarkShareResponse Map(MarkShare share);
}
