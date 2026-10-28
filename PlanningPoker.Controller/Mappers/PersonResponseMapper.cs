using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonResponseMapper : IPersonResponseMapper
{
    public PersonResponse Map(Person person)
    {
        return new PersonResponse { Guid = person.Guid, Name = person.Name, PersonType = person.PersonType };
    }
}
