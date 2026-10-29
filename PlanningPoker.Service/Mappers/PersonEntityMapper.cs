using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public class PersonEntityMapper : IPersonEntityMapper
{
    public PersonEntity Map(Guid guid, string name, string personType)
    {
        return new PersonEntity { Guid = guid, Name = name, PersonType = personType };
    }
}
