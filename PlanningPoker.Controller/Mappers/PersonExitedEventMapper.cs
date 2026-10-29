using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonExitedEventMapper(IPersonResponseMapper personMapper) : IPersonExitedEventMapper
{
    public PersonExitedEvent Map(ExitRoomResult result)
    {
        return new PersonExitedEvent { Person = personMapper.Map(result.Person) };
    }
}
