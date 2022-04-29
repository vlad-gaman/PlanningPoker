using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PlanningPokerUi.Models
{
    public class Room
    {
        private readonly ConcurrentDictionary<Guid, Person> _people;
        private readonly ConcurrentDictionary<Guid, string> _votes;

        public Room(Person person)
        {
            _people = new ConcurrentDictionary<Guid, Person>();
            _votes = new ConcurrentDictionary<Guid, string>();
            AddPerson(person);
        }

        public Guid Guid { get; set; }

        public void AddPerson(Person person)
        {
            _people.TryAdd(person.Guid, person);
        }

        public void RemovePerson(Person person)
        {
            if (_people.TryRemove(person.Guid, out _))
            {
                _votes.TryRemove(person.Guid, out _);
            }
        }

        public bool IsPersonHere(Person person)
        {
            return _people.TryGetValue(person.Guid, out _);
        }

        public IEnumerable<Person> GetPeople()
        {
            return _people.Select(p => p.Value);
        }

        public void Vote(Guid personGuid, string mark)
        {
            _votes.AddOrUpdate(personGuid, mark, (guid, oldMark) => mark);
        }

        public bool DidEveryoneVote()
        {
            return !_people.Any(p => !p.Value.IsObserver && !_votes.ContainsKey(p.Key));
        }

        public void ClearVotes()
        {
            _votes.Clear();
        }

        public IEnumerable<(Guid guid, string mark)> AllVotes()
        {
            return _votes.Where(p =>
            {
                _people.TryGetValue(p.Key, out var person);
                return !person.IsObserver;
            }).Select(p => (p.Key, p.Value));
        }
    }
}
