using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>An in-memory <see cref="IRoomStore"/> that can be told to fail, to see how the code around it copes.</summary>
public sealed class FakeRoomStore : IRoomStore
{
    public Dictionary<string, StoredRoom> Rooms { get; } = [];

    public bool Failing { get; set; }

    public IReadOnlyList<StoredRoom> LoadAll() => [.. Rooms.Values];

    public void Save(StoredRoom room)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        Rooms[room.Room.Id] = room;
    }

    public void Delete(string roomId)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        Rooms.Remove(roomId);
    }
}
