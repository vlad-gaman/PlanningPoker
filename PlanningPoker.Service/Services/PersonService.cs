using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class PersonService(
    IPersonRepository repository,
    IPersonMapper personMapper,
    IPersonSessionMapper sessionMapper,
    IPersonEntityMapper entityMapper,
    TimeProvider time) : IPersonService
{
    public ServiceResponse<Person> Get(string? sessionToken)
    {
        var entity = repository.GetBySessionToken(sessionToken);
        if (entity is null)
        {
            return ServiceResponse<Person>.Failure(ServiceError.NoSession());
        }

        return ServiceResponse<Person>.Success(personMapper.Map(entity));
    }

    public ServiceResponse<PersonSession> Upsert(string? sessionToken, string name, string personType)
    {
        var trimmedName = name.Trim();
        var existing = repository.GetBySessionToken(sessionToken);
        if (existing is not null && repository.Update(existing.Guid, person =>
            {
                person.Name = trimmedName;
                person.PersonType = personType;
                person.LastSeenAt = time.GetUtcNow();
                return sessionMapper.Map(person);
            }, out var updated))
        {
            return ServiceResponse<PersonSession>.Success(updated!);
        }

        var created = entityMapper.Map(Guid.NewGuid(), trimmedName, personType);
        repository.TryAdd(created);
        return ServiceResponse<PersonSession>.Success(sessionMapper.Map(created));
    }
}
