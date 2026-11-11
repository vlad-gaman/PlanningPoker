using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonOffgridEventMapper(IPersonResponseMapper personMapper) : IPersonOffgridEventMapper
{
    public PersonOffgridEvent Map(PersonOffgridResult result)
    {
        return new PersonOffgridEvent { Person = personMapper.Map(result.Person) };
    }
}
