using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class PersonMapper : IPersonMapper
{
    public Person Map(PersonEntity entity)
    {
        return Map(entity, isOffgrid: false);
    }

    public Person Map(PersonEntity entity, bool isOffgrid)
    {
        return new Person { Guid = entity.Guid, Name = entity.Name, PersonType = entity.PersonType, IsOffgrid = isOffgrid };
    }

    public Person Map(PersonEntity entity, bool isOffgrid, string personType)
    {
        return new Person { Guid = entity.Guid, Name = entity.Name, PersonType = personType, IsOffgrid = isOffgrid };
    }

    public Person Map(Person person, string personType)
    {
        return new Person { Guid = person.Guid, Name = person.Name, PersonType = personType, IsOffgrid = person.IsOffgrid };
    }
}
