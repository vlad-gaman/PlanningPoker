using Microsoft.AspNetCore.Http;
using PlanningPokerUi.Models;
using System;
using System.Collections.Concurrent;

namespace PlanningPokerUi.Services
{
    public class PeopleManagerService
    {
        private readonly ConcurrentDictionary<Guid, Person> _people;

        public PeopleManagerService()
        {
            _people = new ConcurrentDictionary<Guid, Person>();
        }

        public bool AddPerson(Guid guid, Person person)
        {
            return _people.TryAdd(guid, person);
        }

        public Person RemovePerson(Guid guid)
        {
            _people.TryRemove(guid, out var person);
            return person;
        }

        public Person GetPerson(Guid guid)
        {
            _people.TryGetValue(guid, out var person);
            return person;
        }

        public Person GetPerson(HttpContext httpContext)
        {
            var guid = Guid.Parse(httpContext.Session.GetString("Guid"));
            return GetPerson(guid);
        }
    }
}
