using PlanningPokerUi.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace PlanningPokerUi.Services
{
    public class RoomsManagerService
    {
        private readonly ConcurrentDictionary<Guid, Room> _rooms;

        public RoomsManagerService()
        {
            _rooms = new ConcurrentDictionary<Guid, Room>();
        }

        public Room GetRoom(Person person)
        {
            return _rooms.FirstOrDefault(p => p.Value.IsPersonHere(person)).Value;
        }

        public Room GetRoom(Guid guid)
        {
            _rooms.TryGetValue(guid, out var room);
            return room;
        }

        public bool DoesRoomExist(Guid guid)
        {
            return _rooms.TryGetValue(guid, out _);
        }

        public Guid CreateRoom(Person person)
        {
            Guid guid = Guid.Empty;
            var created = false;
            var index = 0;
            var room = new Room(person);

            while (!created)
            {
                guid = Guid.NewGuid();
                created = _rooms.TryAdd(guid, room);
                if (index > 0)
                {
                    return Guid.Empty;
                }
                index++;
            }

            room.Guid = guid;
            return guid;
        }

        public bool JoinRoom(Person person, Guid guid)
        {
            if (_rooms.TryGetValue(guid, out Room room))
            {
                room.AddPerson(person);
                return true;
            }

            return false;
        }

        public void ExitRoom(Person person, Guid guid)
        {
            if (_rooms.TryGetValue(guid, out Room room))
            {
                room.RemovePerson(person);             
            }
        }
    }
}
