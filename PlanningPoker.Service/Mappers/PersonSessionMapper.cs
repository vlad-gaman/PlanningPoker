using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class PersonSessionMapper(IPersonMapper personMapper) : IPersonSessionMapper
{
    public PersonSession Map(PersonEntity entity)
    {
        return new PersonSession { Person = personMapper.Map(entity), SessionToken = entity.SessionToken! };
    }
}
