using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Fakes;

public static class RoomRepositoryTestExtensions
{
    /// <summary>Sets a room up directly, reported as a change; it does not stamp the room's last activity (the Service layer does).</summary>
    public static void Edit(this IRoomRepository rooms, string roomId, Action<RoomEntity> change)
    {
        rooms.Update(roomId, room =>
        {
            change(room);
            return RoomUpdate.Changed(0);
        }, out _);
    }
}
