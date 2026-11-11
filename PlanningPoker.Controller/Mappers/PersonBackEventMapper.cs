using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonBackEventMapper(IPersonResponseMapper personMapper) : IPersonBackEventMapper
{
    public PersonBackEvent Map(JoinRoomResult result)
    {
        return new PersonBackEvent { Person = personMapper.Map(result.Person) };
    }
}
