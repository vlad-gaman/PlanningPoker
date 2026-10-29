using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class PersonMapper : IPersonMapper
{
    public Person Map(PersonEntity entity)
    {
        return new Person { Guid = entity.Guid, Name = entity.Name, PersonType = entity.PersonType };
    }
}
