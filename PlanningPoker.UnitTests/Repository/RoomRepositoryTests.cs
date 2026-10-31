using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Repository;

public class RoomRepositoryTests
{
    private static RoomRepository NewRepository() => new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));

    private static RoomEntity NewRoom(string id = "room", Guid? owner = null)
    {
        var ownerGuid = owner ?? Guid.NewGuid();
        return new RoomEntity { Id = id, OwnerGuid = ownerGuid, MemberGuids = [ownerGuid] };
    }

    [Fact]
    public void Get_Unknown_ReturnsNull()
    {
        Assert.Null(NewRepository().Get("nope"));
    }

    [Fact]
    public void Get_ReturnsSnapshotCopy()
    {
        var repository = NewRepository();
        repository.TryAdd(NewRoom());

        var snapshot = repository.Get("room")!;
        snapshot.MemberGuids.Add(Guid.NewGuid());

        Assert.Single(repository.Get("room")!.MemberGuids);
    }

    [Fact]
    public void TryAdd_Duplicate_ReturnsFalse()
    {
        var repository = NewRepository();

        Assert.True(repository.TryAdd(NewRoom()));
        Assert.False(repository.TryAdd(NewRoom()));
    }

    [Fact]
    public void TryAdd_StoresCopy()
    {
        var repository = NewRepository();
        var room = NewRoom();
        repository.TryAdd(room);

        room.MemberGuids.Add(Guid.NewGuid());

        Assert.Single(repository.Get("room")!.MemberGuids);
    }

    [Fact]
    public void Remove_RemovesOnlyExisting()
    {
        var repository = NewRepository();
        repository.TryAdd(NewRoom());

        Assert.True(repository.Remove("room"));
        Assert.False(repository.Remove("room"));
    }

    [Fact]
    public void Update_Unknown_ReturnsFalse()
    {
        var found = NewRepository().Update("nope", _ => 1, out var result);

        Assert.False(found);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Update_MutatesStoredRoomAndReturnsResult()
    {
        var repository = NewRepository();
        repository.TryAdd(NewRoom());
        var person = Guid.NewGuid();

        var found = repository.Update("room", room =>
        {
            room.MemberGuids.Add(person);
            return room.MemberGuids.Count;
        }, out var count);

        Assert.True(found);
        Assert.Equal(2, count);
        Assert.Contains(person, repository.Get("room")!.MemberGuids);
    }

    [Fact]
    public void Update_ConcurrentCallers_AreAtomic()
    {
        var repository = NewRepository();
        repository.TryAdd(NewRoom());

        var counter = Guid.NewGuid();

        Parallel.For(0, 500, _ => repository.Update("room", room =>
        {
            room.Votes[counter] = (int.Parse(room.Votes.GetValueOrDefault(counter, "0")) + 1).ToString();
            return 0;
        }, out _));

        Assert.Equal("500", repository.Get("room")!.Votes[counter]);
    }
}
