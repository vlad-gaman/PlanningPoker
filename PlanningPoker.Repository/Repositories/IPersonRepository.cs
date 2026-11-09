using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public interface IPersonRepository
{
    /// <summary>Returns the live stored entity, not a copy. Only <see cref="Update{T}"/> may mutate it.</summary>
    PersonEntity? Get(Guid guid);

    bool TryAdd(PersonEntity person);

    bool Remove(Guid guid);

    /// <summary>A snapshot of the stored GUIDs.</summary>
    IReadOnlyCollection<Guid> Guids();

    /// <summary>
    /// Removes the person only when <paramref name="shouldRemove"/> holds, deciding and removing under the person lock. The
    /// person is flagged removed first, so a later <see cref="Update{T}"/> on a stale reference fails.
    /// </summary>
    bool RemoveIf(Guid guid, Func<PersonEntity, bool> shouldRemove);

    /// <summary>The only way to mutate a person. Runs under a per-person lock; returns false when the person does not exist.</summary>
    bool Update<T>(Guid guid, Func<PersonEntity, T> mutate, out T? result);
}
