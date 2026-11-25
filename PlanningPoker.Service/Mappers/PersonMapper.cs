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
        return Map(entity, isOffgrid, entity.PersonType);
    }

    public Person Map(PersonEntity entity, bool isOffgrid, string personType)
    {
        return new Person { Guid = entity.Guid, Name = entity.Name, PersonType = personType, CardBack = entity.CardBack, IsOffgrid = isOffgrid };
    }

    public Person Map(Person person, string personType)
    {
        return new Person { Guid = person.Guid, Name = person.Name, PersonType = personType, CardBack = person.CardBack, IsOffgrid = person.IsOffgrid };
    }
}
