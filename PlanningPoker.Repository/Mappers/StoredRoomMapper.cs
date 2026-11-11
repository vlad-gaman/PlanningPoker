using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Repository.Mappers;

public class StoredRoomMapper : IStoredRoomMapper
{
    public StoredRoom Map(RoomEntity room, IPersonRepository people, DateTimeOffset savedAt)
    {
        var stored = new List<PersonEntity>();
        foreach (var guid in room.MemberGuids.Append(room.OwnerGuid).Distinct())
        {
            if (people.Update(guid, person => new PersonEntity
            {
                Guid = person.Guid,
                Name = person.Name,
                PersonType = person.PersonType,
                LastSeenAt = person.LastSeenAt
            }, out var copy) && copy is not null)
            {
                stored.Add(copy);
            }
        }

        // Connections belong to the running process and mean nothing after a restart, so they are never stored.
        room.MemberConnections = [];
        return new StoredRoom { Room = room, People = stored, SavedAt = savedAt };
    }
}
