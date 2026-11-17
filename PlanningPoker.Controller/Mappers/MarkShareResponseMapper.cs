using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class MarkShareResponseMapper : IMarkShareResponseMapper
{
    public MarkShareResponse Map(MarkShare share)
    {
        return new MarkShareResponse { Mark = share.Mark, Percentage = share.Percentage };
    }
}
