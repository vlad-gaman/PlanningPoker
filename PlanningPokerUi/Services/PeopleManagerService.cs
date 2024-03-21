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
            var guidStr = httpContext.Session.GetString("Guid");
            if (string.IsNullOrWhiteSpace(guidStr))
            {
                return null;
            }
            var guid = Guid.Parse(guidStr);
            return GetPerson(guid);
        }

        public Person CreatePerson(HttpContext httpContext)
        {
            var guidAsString = httpContext.Session.GetString("Guid");
            var newGuid = Guid.NewGuid();
            httpContext.Session.SetString("Guid", newGuid.ToString());
            var person = new Person()
            {
                Guid = newGuid
            };
            
            AddPerson(newGuid, person);

            return person;
        }

        public bool DoesPersonExist(Person person)
        {
            if (person?.Guid == null || person?.Guid == Guid.Empty)
            {
                return false;
            }

            if (_people.TryGetValue(person.Guid, out var person1))
            {
                return !string.IsNullOrEmpty(person1.Name);
            }

            return false;
        }
    }
}
