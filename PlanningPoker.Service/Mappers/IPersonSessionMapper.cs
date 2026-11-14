using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IPersonSessionMapper
{
    PersonSession Map(PersonEntity entity);
}
