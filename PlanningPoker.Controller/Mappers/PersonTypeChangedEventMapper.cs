using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class PersonTypeChangedEventMapper(IPersonResponseMapper personMapper, IVoteStatusResponseMapper voteStatusMapper) : IPersonTypeChangedEventMapper
{
    public PersonTypeChangedEvent Map(PersonTypeChangeResult result)
    {
        return new PersonTypeChangedEvent { Person = personMapper.Map(result.Person), VoteStatus = voteStatusMapper.Map(result.VoteStatus) };
    }
}
