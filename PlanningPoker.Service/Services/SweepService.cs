using Microsoft.Extensions.Options;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class SweepService(IRoomRepository rooms, IPersonRepository people, IOptions<SweepOptions> options, TimeProvider time) : ISweepService
{
    public SweepResult Sweep()
    {
        var cutoff = time.GetUtcNow() - options.Value.AbandonedAfter;
        var removedRooms = 0;
        foreach (var roomId in rooms.Ids())
        {
            // Disposing inside the room lock means a Join that comes later is refused, so the room cannot gain a member after the check.
            var abandoned = rooms.Update(roomId, room =>
            {
                if (room.MemberConnections.Count > 0 || room.CreatedAt >= cutoff)
                {
                    return false;
                }

                room.IsDisposed = true;
                return true;
            }, out var disposed) && disposed;
            if (abandoned && rooms.Remove(roomId))
            {
                removedRooms++;
            }
        }

        // Surviving rooms are read after the room pass, so a person is never kept only by a room that was just swept.
        var referenced = new HashSet<Guid>();
        foreach (var roomId in rooms.Ids())
        {
            var room = rooms.Get(roomId);
            if (room is null)
            {
                continue;
            }

            referenced.Add(room.OwnerGuid);
            referenced.UnionWith(room.MemberGuids);
        }

        var removedPeople = 0;
        foreach (var guid in people.Guids())
        {
            if (!referenced.Contains(guid) && people.RemoveIf(guid, person => person.Connections.Count == 0 && person.LastSeenAt < cutoff))
            {
                removedPeople++;
            }
        }

        return new SweepResult { Rooms = removedRooms, People = removedPeople };
    }
}
