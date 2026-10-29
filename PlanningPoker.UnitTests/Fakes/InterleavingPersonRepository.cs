using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>
/// Wraps a real person repository. <see cref="AfterNextUpdate"/> runs once, right after the next Update returns and
/// before the caller continues, so a test can run a competing operation between two steps of one operation.
/// </summary>
public sealed class InterleavingPersonRepository(IPersonRepository inner) : IPersonRepository
{
    public Action? AfterNextUpdate { get; set; }

    public PersonEntity? Get(Guid guid) => inner.Get(guid);

    public bool TryAdd(PersonEntity person) => inner.TryAdd(person);

    public bool Remove(Guid guid) => inner.Remove(guid);

    public bool Update<T>(Guid guid, Func<PersonEntity, T> mutate, out T? result)
    {
        var found = inner.Update(guid, mutate, out result);
        var hook = AfterNextUpdate;
        AfterNextUpdate = null;
        hook?.Invoke();
        return found;
    }
}
