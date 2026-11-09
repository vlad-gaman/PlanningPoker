using System.Collections.Concurrent;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly ConcurrentDictionary<Guid, PersonEntity> _people = new();

    public PersonEntity? Get(Guid guid)
    {
        return _people.GetValueOrDefault(guid);
    }

    public bool TryAdd(PersonEntity person)
    {
        return _people.TryAdd(person.Guid, person);
    }

    public bool Remove(Guid guid)
    {
        return _people.TryRemove(guid, out _);
    }

    public IReadOnlyCollection<Guid> Guids()
    {
        return [.. _people.Keys];
    }

    public bool RemoveIf(Guid guid, Func<PersonEntity, bool> shouldRemove)
    {
        if (!_people.TryGetValue(guid, out var person))
        {
            return false;
        }

        lock (person)
        {
            if (person.IsRemoved || !shouldRemove(person))
            {
                return false;
            }

            person.IsRemoved = true;
            return _people.TryRemove(guid, out _);
        }
    }

    public bool Update<T>(Guid guid, Func<PersonEntity, T> mutate, out T? result)
    {
        if (!_people.TryGetValue(guid, out var person))
        {
            result = default;
            return false;
        }

        lock (person)
        {
            if (person.IsRemoved)
            {
                result = default;
                return false;
            }

            result = mutate(person);
            return true;
        }
    }
}
