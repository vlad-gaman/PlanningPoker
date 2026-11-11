using Microsoft.Extensions.Logging.Abstractions;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomRestorerTests
{
    private static readonly DateTimeOffset SavedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeRoomStore _store = new();
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly Guid _ann = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    private RoomRestorer NewRestorer() => new(_store, _rooms, _people, NullLogger<RoomRestorer>.Instance);

    private void Store(Action<RoomEntity>? shape = null)
    {
        var room = new RoomEntity
        {
            Id = "r",
            OwnerGuid = _ann,
            MemberGuids = [_ann, _bob],
            MemberConnections = new Dictionary<Guid, HashSet<string>> { [_ann] = ["c1"] },
            DroppedAt = new Dictionary<Guid, DateTimeOffset> { [_bob] = SavedAt - TimeSpan.FromMinutes(3) },
            Votes = new Dictionary<Guid, string> { [_ann] = "5" },
            VotingEnabled = true,
            Revision = 4,
            CountdownRevision = 4,
            CountdownRemaining = 3
        };
        shape?.Invoke(room);
        _store.Rooms["r"] = new StoredRoom
        {
            Room = room,
            SavedAt = SavedAt,
            People =
            [
                new PersonEntity { Guid = _ann, Name = "Ann", PersonType = "dev", Connections = { ["c1"] = "r" } },
                new PersonEntity { Guid = _bob, Name = "Bob", PersonType = "test" }
            ]
        };
    }

    [Fact]
    public void Restore_PutsTheRoomBackWithItsVotesAndPeople()
    {
        Store();

        Assert.Equal(1, NewRestorer().Restore());

        var room = _rooms.Get("r")!;
        Assert.Equal("5", room.Votes[_ann]);
        Assert.True(room.VotingEnabled);
        Assert.Equal("Ann", _people.Get(_ann)!.Name);
        Assert.Empty(_people.Get(_ann)!.Connections);
    }

    [Fact]
    public void Restore_EveryMemberIsOffgrid_WithNoConnections()
    {
        Store();

        NewRestorer().Restore();

        var room = _rooms.Get("r")!;
        Assert.Empty(room.MemberConnections);
        Assert.Equivalent(new[] { _ann, _bob }, room.OffgridGuids);
    }

    [Fact]
    public void Restore_MemberWhoWasOnline_IsDroppedWhenTheRoomWasLastWritten_AndEarlierDropsKeepTheirTime()
    {
        Store();

        NewRestorer().Restore();

        var room = _rooms.Get("r")!;
        Assert.Equal(SavedAt, room.DroppedAt[_ann]);
        Assert.Equal(SavedAt - TimeSpan.FromMinutes(3), room.DroppedAt[_bob]);
    }

    [Fact]
    public void Restore_RunningCountdown_IsCancelledAndVotingStaysOpen()
    {
        Store();

        NewRestorer().Restore();

        var room = _rooms.Get("r")!;
        Assert.False(room.CountdownRunning);
        Assert.Equal(0, room.CountdownRemaining);
        Assert.True(room.VotingEnabled);
    }

    [Fact]
    public void Restore_RevealedRound_StaysRevealed()
    {
        Store(room =>
        {
            room.VotingEnabled = false;
            room.Revealed = new RevealedVotesEntity { Votes = { [_ann] = new RevealedVoteEntity { Mark = "5", PersonType = "dev" } } };
        });

        NewRestorer().Restore();

        Assert.NotNull(_rooms.Get("r")!.Revealed);
    }

    [Fact]
    public void Restore_RoomWithMembers_AwaitsTheirReturn()
    {
        Store();

        NewRestorer().Restore();

        Assert.True(_rooms.Get("r")!.AwaitingReturn);
    }

    [Fact]
    public void Restore_DisposedRoom_IsSkipped()
    {
        Store(room => room.IsDisposed = true);

        Assert.Equal(0, NewRestorer().Restore());
        Assert.Null(_rooms.Get("r"));
    }

    [Fact]
    public void Restore_EmptyStore_RestoresNothing()
    {
        Assert.Equal(0, NewRestorer().Restore());
    }
}
