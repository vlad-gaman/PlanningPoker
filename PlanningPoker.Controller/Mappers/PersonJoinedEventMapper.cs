using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonJoinedEventMapper(IPersonResponseMapper personMapper) : IPersonJoinedEventMapper
{
    public PersonJoinedEvent Map(JoinRoomResult result)
    {
        return new PersonJoinedEvent
        {
            Person = personMapper.Map(result.Person),
            Vote = result.VoteStatus.Votes.GetValueOrDefault(result.Person.Guid),
            PersonType = result.Person.PersonType
        };
    }
}
