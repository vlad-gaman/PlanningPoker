using System.Collections.Concurrent;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly ConcurrentDictionary<Guid, PersonEntity> _people = new();

    /// <summary>Index from session token to person, kept in step with <see cref="_people"/>. A person without a token is not in it.</summary>
    private readonly ConcurrentDictionary<string, PersonEntity> _bySessionToken = new(StringComparer.Ordinal);

    public PersonEntity? Get(Guid guid)
    {
        return _people.GetValueOrDefault(guid);
    }

    public PersonEntity? GetBySessionToken(string? sessionToken)
    {
        return string.IsNullOrEmpty(sessionToken) ? null : _bySessionToken.GetValueOrDefault(sessionToken);
    }

    public bool TryAdd(PersonEntity person)
    {
        var token = person.SessionToken;
        if (!string.IsNullOrEmpty(token) && !_bySessionToken.TryAdd(token, person))
        {
            return false;
        }

        if (_people.TryAdd(person.Guid, person))
        {
            return true;
        }

        Unindex(person);
        return false;
    }

    public bool Remove(Guid guid)
    {
        if (!_people.TryRemove(guid, out var person))
        {
            return false;
        }

        Unindex(person);
        return true;
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
            if (!_people.TryRemove(guid, out _))
            {
                return false;
            }

            Unindex(person);
            return true;
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

    /// <summary>Drops the person's token from the index, but only while the index still points at this very person.</summary>
    private void Unindex(PersonEntity person)
    {
        if (!string.IsNullOrEmpty(person.SessionToken))
        {
            _bySessionToken.TryRemove(new KeyValuePair<string, PersonEntity>(person.SessionToken, person));
        }
    }
}
