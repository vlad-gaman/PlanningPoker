using Microsoft.Extensions.Logging;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Service.Services;

public class RoomRestorer(IRoomStore store, IRoomRepository rooms, IPersonRepository people, IRoomMembership membership, ILogger<RoomRestorer> logger) : IRoomRestorer
{
    public int Restore()
    {
        var restored = 0;
        foreach (var stored in store.LoadAll())
        {
            var room = stored.Room;
            if (room.IsDisposed)
            {
                continue;
            }

            foreach (var person in stored.People)
            {
                person.Connections = [];
                person.IsRemoved = false;
                people.TryAdd(person);

                // A room saved before roles lived in it has none: the profile's type is the role the member had.
                if (room.MemberGuids.Contains(person.Guid))
                {
                    room.Roles.TryAdd(person.Guid, person.PersonType);
                }
            }

            // Nobody is connected after a restart. The members who were online count as dropped when the room was last written,
            // so the downtime runs against their removal window; members who had already dropped keep their drop time.
            room.MemberConnections = [];
            room.OffgridGuids = [];
            foreach (var guid in room.MemberGuids)
            {
                membership.Drop(room, guid, stored.SavedAt);
                membership.GoOffgrid(room, guid);
            }

            // A running countdown is not resumed: voting stays open and the next change decides again.
            room.CountdownRevision = null;
            room.CountdownRemaining = 0;
            room.AwaitingReturn = room.MemberGuids.Count > 0;

            if (rooms.TryAdd(room))
            {
                restored++;
            }
        }

        logger.LogInformation("Restored {Rooms} rooms from the store", restored);
        return restored;
    }
}
