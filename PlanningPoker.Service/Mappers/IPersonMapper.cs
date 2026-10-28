using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IPersonMapper
{
    Person Map(PersonEntity entity);
}
