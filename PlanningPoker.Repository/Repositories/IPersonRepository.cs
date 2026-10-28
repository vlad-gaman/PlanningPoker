using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public interface IPersonRepository
{
    PersonEntity? Get(Guid guid);

    bool TryAdd(PersonEntity person);

    bool Remove(Guid guid);
}
