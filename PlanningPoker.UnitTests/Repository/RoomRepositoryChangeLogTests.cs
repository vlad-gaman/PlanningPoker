using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;

namespace PlanningPoker.UnitTests.Repository;

public class RoomRepositoryChangeLogTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly RoomChangeLog _log = new();
    private readonly RoomRepository _rooms;

    public RoomRepositoryChangeLogTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()), _log, _time);
    }

    private List<string> Drain()
    {
        var ids = new List<string>();
        while (_log.Changes.TryRead(out var id))
        {
            ids.Add(id);
        }

        return ids;
    }

    private void AddRoom() => _rooms.TryAdd(new RoomEntity { Id = "r", OwnerGuid = Guid.NewGuid(), CreatedAt = _time.GetUtcNow() });

    [Fact]
    public void TryAdd_ReportsTheRoomAndStartsItsActivityAtCreation()
    {
        AddRoom();

        Assert.Equal(["r"], Drain());
        Assert.Equal(_time.GetUtcNow(), _rooms.Get("r")!.LastActivityAt);
    }

    [Fact]
    public void TryAdd_DuplicateId_ReportsNothing()
    {
        AddRoom();
        Drain();

        AddRoom();

        Assert.Empty(Drain());
    }

    [Fact]
    public void Update_ChangingTheRoom_ReportsItAndStampsTheActivity()
    {
        AddRoom();
        Drain();
        _time.Advance(TimeSpan.FromMinutes(5));

        _rooms.Update("r", room => room.VotingEnabled = true, out _);

        Assert.Equal(["r"], Drain());
        Assert.Equal(_time.GetUtcNow(), _rooms.Get("r")!.LastActivityAt);
    }

    [Fact]
    public void Update_ChangingNothing_ReportsNothingAndKeepsTheActivity()
    {
        AddRoom();
        Drain();
        var activity = _rooms.Get("r")!.LastActivityAt;
        _time.Advance(TimeSpan.FromMinutes(5));

        _rooms.Update("r", room => room.VotingEnabled, out _);

        Assert.Empty(Drain());
        Assert.Equal(activity, _rooms.Get("r")!.LastActivityAt);
    }

    [Fact]
    public void Remove_ReportsTheRoomOnlyWhenItExisted()
    {
        AddRoom();
        Drain();

        Assert.True(_rooms.Remove("r"));
        Assert.False(_rooms.Remove("r"));

        Assert.Equal(["r"], Drain());
    }
}
