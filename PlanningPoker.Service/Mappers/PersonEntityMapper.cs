using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Service.Mappers;

public class PersonEntityMapper(TimeProvider time, ISessionTokenGenerator tokens) : IPersonEntityMapper
{
    public PersonEntity Map(Guid guid, string name, string personType, string cardBack)
    {
        return new PersonEntity { Guid = guid, SessionToken = tokens.Generate(), Name = name, PersonType = personType, CardBack = cardBack, LastSeenAt = time.GetUtcNow() };
    }
}
