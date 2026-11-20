using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class RoomUpdaterTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly RoomChangeLog _log = new();
    private readonly RoomRepository _rooms;
    private readonly RoomUpdater _updater;

    public RoomUpdaterTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(), _log);
        _updater = new RoomUpdater(_rooms, _time);
        _rooms.TryAdd(new RoomEntity { Id = "r", OwnerGuid = Guid.NewGuid(), CreatedAt = _time.GetUtcNow() });
        _log.Changes.TryRead(out _);
    }

    [Fact]
    public void Update_ReportingAChange_StampsTheActivityAndReportsTheRoom()
    {
        _time.Advance(TimeSpan.FromMinutes(5));

        _updater.Update("r", room => RoomUpdate.Changed(room.VotingEnabled = true), out _);

        Assert.Equal(_time.GetUtcNow(), _rooms.Get("r")!.LastActivityAt);
        Assert.True(_log.Changes.TryRead(out var id));
        Assert.Equal("r", id);
    }

    [Fact]
    public void Update_ReportingNoChange_KeepsTheActivityAndReportsNothing()
    {
        var activity = _rooms.Get("r")!.LastActivityAt;
        _time.Advance(TimeSpan.FromMinutes(5));

        _updater.Update("r", room => RoomUpdate.Unchanged(room.VotingEnabled), out _);

        Assert.Equal(activity, _rooms.Get("r")!.LastActivityAt);
        Assert.False(_log.Changes.TryRead(out _));
    }

    [Fact]
    public void Update_ReturnsTheResultOfTheMutation()
    {
        var found = _updater.Update("r", room => RoomUpdate.Changed(room.Id), out var result);

        Assert.True(found);
        Assert.Equal("r", result);
    }

    [Fact]
    public void Update_UnknownRoom_ReturnsFalseAndReportsNothing()
    {
        var found = _updater.Update("nope", _ => RoomUpdate.Changed(0), out var result);

        Assert.False(found);
        Assert.Equal(0, result);
        Assert.False(_log.Changes.TryRead(out _));
    }
}
