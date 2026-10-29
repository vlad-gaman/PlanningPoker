using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class PersonService(IPersonRepository repository, IPersonMapper personMapper, IPersonEntityMapper entityMapper) : IPersonService
{
    public ServiceResponse<Person> Get(Guid? guid)
    {
        var entity = guid is null ? null : repository.Get(guid.Value);
        if (entity is null)
        {
            return ServiceResponse<Person>.Failure(ServiceError.NoSession());
        }

        return ServiceResponse<Person>.Success(personMapper.Map(entity));
    }

    public ServiceResponse<Person> Upsert(Guid? existingGuid, string name, string personType)
    {
        var trimmedName = name.Trim();
        if (existingGuid is not null && repository.Update(existingGuid.Value, existing =>
            {
                existing.Name = trimmedName;
                existing.PersonType = personType;
                return personMapper.Map(existing);
            }, out var updated))
        {
            return ServiceResponse<Person>.Success(updated!);
        }

        var created = entityMapper.Map(Guid.NewGuid(), trimmedName, personType);
        repository.TryAdd(created);
        return ServiceResponse<Person>.Success(personMapper.Map(created));
    }
}
