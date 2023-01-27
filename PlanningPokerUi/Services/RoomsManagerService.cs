using PlanningPokerUi.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace PlanningPokerUi.Services
{
    public class RoomsManagerService
    {
        private readonly ConcurrentDictionary<string, Room> _rooms;

        public RoomsManagerService()
        {
            _rooms = new ConcurrentDictionary<string, Room>();
        }

        public Room GetRoom(Person person)
        {
            return _rooms.FirstOrDefault(p => p.Value.IsPersonHere(person)).Value;
        }

        public Room GetRoom(string guid)
        {
            _rooms.TryGetValue(guid, out var room);
            return room;
        }

        public bool DoesRoomExist(string guid)
        {
            if (!string.IsNullOrWhiteSpace(guid))
            {
                return _rooms.TryGetValue(guid, out _);
            }
            return false;
        }

        public string CreateRoom(Person person, bool useFunName)
        {
            string guid = string.Empty;
            var created = false;
            var index = 0;
            var room = new Room(person);

            while (!created)
            {
                guid = useFunName ? RoomNameGenerator.Generate() : Guid.NewGuid().ToString();
                created = _rooms.TryAdd(guid, room);
                if (index > 0)
                {
                    return string.Empty;
                }
                index++;
            }

            room.Guid = guid;
            return guid;
        }

        public bool JoinRoom(Person person, string guid, out Room room)
        {
            if (_rooms.TryGetValue(guid, out room))
            {
                room.AddPerson(person);
                return true;
            }

            return false;
        }

        public void ExitRoom(Person person, string guid)
        {
            if (_rooms.TryGetValue(guid, out Room room))
            {
                room.RemovePerson(person);
            }
        }
    }
}
