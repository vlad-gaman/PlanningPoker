using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Service.Services;

public class RoomUpdater(IRoomRepository rooms, TimeProvider time) : IRoomUpdater
{
    public bool Update<T>(string roomId, Func<RoomEntity, RoomUpdate<T>> mutate, out T? result)
    {
        // Stamped inside the room lock, before the repository reports the change, so the stored room carries the stamp.
        return rooms.Update(roomId, room =>
        {
            var update = mutate(room);
            if (update.Changed)
            {
                room.LastActivityAt = time.GetUtcNow();
            }

            return update;
        }, out result);
    }
}
