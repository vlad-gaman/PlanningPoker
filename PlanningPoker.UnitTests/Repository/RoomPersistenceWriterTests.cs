using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Repository;

public class RoomPersistenceWriterTests
{
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(30);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly RoomChangeLog _log = new();
    private readonly PersonRepository _people = new();
    private readonly FakeRoomStore _store = new();
    private readonly FakeLogger<RoomPersistenceWriter> _logger = new();
    private readonly RoomRepository _rooms;
    private readonly RoomPersistenceWriter _writer;

    public RoomPersistenceWriterTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()), _log, _time);
        _writer = new RoomPersistenceWriter(_log, _rooms, _people, _store, new StoredRoomMapper(), Options.Create(new PersistenceOptions { HeartbeatInterval = Heartbeat }), _time, _logger);
    }

    private Guid AddRoom(string id = "r")
    {
        var owner = Guid.NewGuid();
        _people.TryAdd(new PersonEntity { Guid = owner, Name = "Ann", PersonType = "dev" });
        _rooms.TryAdd(new RoomEntity { Id = id, OwnerGuid = owner, MemberGuids = [owner], CreatedAt = _time.GetUtcNow() });
        return owner;
    }

    [Fact]
    public void Flush_SavesTheLatestStateOfAChangedRoomOnce()
    {
        var owner = AddRoom();
        _rooms.Update("r", room => room.Votes[owner] = "3", out _);
        _rooms.Update("r", room => room.Votes[owner] = "8", out _);

        _writer.Flush();

        var stored = Assert.Single(_store.Rooms.Values);
        Assert.Equal("8", stored.Room.Votes[owner]);
        Assert.Equal("Ann", Assert.Single(stored.People).Name);
        Assert.Equal(_time.GetUtcNow(), stored.SavedAt);
    }

    [Fact]
    public void Flush_RoomThatWasRemoved_IsDeletedFromTheStore()
    {
        AddRoom();
        _writer.Flush();
        _rooms.Remove("r");

        _writer.Flush();

        Assert.Empty(_store.Rooms);
    }

    [Fact]
    public async Task Flush_FailingStore_DoesNotThrowAndTheNextHeartbeatWritesTheRoomAgain()
    {
        _store.Failing = true;
        AddRoom();
        await _writer.StartAsync(CancellationToken.None);
        await Eventually(() => !_log.Changes.TryPeek(out _));
        Assert.Empty(_store.Rooms);

        _store.Failing = false;
        await Eventually(() =>
        {
            _time.Advance(Heartbeat);
            return _store.Rooms.Count == 1;
        });

        await _writer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Heartbeat_WritesRoomsWithSomeoneConnectedAgain()
    {
        var owner = AddRoom();
        _rooms.Update("r", room => room.MemberConnections[owner] = ["c1"], out _);
        _writer.Flush();
        var first = _store.Rooms["r"].SavedAt;
        await _writer.StartAsync(CancellationToken.None);

        await Eventually(() =>
        {
            _time.Advance(Heartbeat);
            return _store.Rooms["r"].SavedAt > first;
        });

        await _writer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Stop_WritesTheChangesStillPending()
    {
        await _writer.StartAsync(CancellationToken.None);
        AddRoom();

        await _writer.StopAsync(CancellationToken.None);

        Assert.True(_store.Rooms.Count == 1, string.Join(" | ", _logger.Collector.GetSnapshot().Select(r => r.Message + r.Exception)) + " pending=" + _log.Changes.Count);
    }

    private static async Task Eventually(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(20);
        }
    }
}
