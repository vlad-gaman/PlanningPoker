using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public interface IPersonRepository
{
    /// <summary>Returns the live stored entity, not a copy. Only <see cref="Update{T}"/> may mutate it.</summary>
    PersonEntity? Get(Guid guid);

    bool TryAdd(PersonEntity person);

    bool Remove(Guid guid);

    /// <summary>The only way to mutate a person. Runs under a per-person lock; returns false when the person does not exist.</summary>
    bool Update<T>(Guid guid, Func<PersonEntity, T> mutate, out T? result);
}
