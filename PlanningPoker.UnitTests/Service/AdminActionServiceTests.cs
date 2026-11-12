using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class AdminActionServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly RoomChangeLog _changes = new();
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms;
    private readonly FakeRoomAdminNotifier _admin = new();
    private readonly FakeRoomPresenceNotifier _presence = new();
    private readonly FakeRoomCountdownNotifier _countdown = new();
    private readonly FakeRoomTimerService _timer = new();
    private readonly FakeIssueLog _issues = new();
    private readonly AdminActionService _service;

    private readonly Guid _ann = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _cat = Guid.NewGuid();

    public AdminActionServiceTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()), _changes, _time);
        var roomService = RoomServiceFactory.Create(_rooms, _people, time: _time);
        _service = new AdminActionService(_rooms, _people, new PersonMapper(), new ExitRoomResultMapper(), roomService, _admin, _presence, _countdown, _timer, _issues);
        AddPerson(_ann, "Ann", "dev", "conn-ann");
        AddPerson(_bob, "Bob", "test", "conn-bob");
        AddPerson(_cat, "Cat", "dev", "conn-cat");
    }

    private void AddPerson(Guid guid, string name, string type, string connection)
    {
        _people.TryAdd(new PersonEntity { Guid = guid, Name = name, PersonType = type, Connections = new Dictionary<string, string> { [connection] = "blue-fox" } });
    }

    /// <summary>Ann (owner), Bob and Cat, all connected.</summary>
    private void AddRoom(Action<RoomEntity>? change = null)
    {
        var room = new RoomEntity
        {
            Id = "blue-fox",
            OwnerGuid = _ann,
            MemberGuids = [_ann, _bob, _cat],
            MemberConnections = new Dictionary<Guid, HashSet<string>>
            {
                [_ann] = ["conn-ann"],
                [_bob] = ["conn-bob", "conn-bob-2"],
                [_cat] = ["conn-cat"]
            },
            VotingEnabled = true,
            CreatedAt = _time.GetUtcNow(),
            LastActivityAt = _time.GetUtcNow()
        };
        room.Configuration.CountdownSeconds = 5;
        change?.Invoke(room);
        _rooms.TryAdd(room);
    }

    [Fact]
    public async Task CloseRoom_UnknownRoom_IsRoomNotFound()
    {
        var result = await _service.CloseRoomAsync("nowhere");

        Assert.Equal(ErrorCodes.RoomNotFound, result.Errors[0].Code);
        Assert.Empty(_admin.Closed);
    }

    [Fact]
    public async Task CloseRoom_RemovesTheRoomAndTellsItsConnections()
    {
        AddRoom();

        var result = await _service.CloseRoomAsync("blue-fox");

        Assert.True(result.IsSuccess);
        Assert.Null(_rooms.Get("blue-fox"));
        var (roomId, connections) = Assert.Single(_admin.Closed);
        Assert.Equal("blue-fox", roomId);
        Assert.Equal(["conn-ann", "conn-bob", "conn-bob-2", "conn-cat"], connections.Order());
    }

    [Fact]
    public async Task CloseRoom_ReportsTheRemovalSoTheStoreDeletesIt()
    {
        AddRoom();
        while (_changes.Changes.TryRead(out _))
        {
        }

        await _service.CloseRoomAsync("blue-fox");

        Assert.True(_changes.Changes.TryRead(out var changed));
        Assert.Equal("blue-fox", changed);
    }

    [Fact]
    public async Task CloseRoom_RecordsTheActionInTheIssuesLog()
    {
        AddRoom();

        await _service.CloseRoomAsync("blue-fox");

        var entry = Assert.Single(_issues.Recorded);
        Assert.Equal(IssueLevel.Info, entry.Level);
        Assert.Equal(IssueLogConstants.AdminSource, entry.Source);
        Assert.Contains("closed room \"blue-fox\" (3 people)", entry.Message);
    }

    [Fact]
    public async Task CloseRoom_ARoomNobodyJoinedYet_Closes()
    {
        AddRoom(room => { room.MemberGuids = []; room.MemberConnections = []; });

        var result = await _service.CloseRoomAsync("blue-fox");

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.Single(_admin.Closed).ConnectionIds);
    }

    [Fact]
    public async Task RemovePerson_UnknownRoom_IsRoomNotFound()
    {
        var result = await _service.RemovePersonAsync("nowhere", _bob);

        Assert.Equal(ErrorCodes.RoomNotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task RemovePerson_UnknownPerson_IsPersonNotFound()
    {
        AddRoom();

        var result = await _service.RemovePersonAsync("blue-fox", Guid.NewGuid());

        Assert.Equal(ErrorCodes.PersonNotFound, result.Errors[0].Code);
        Assert.Empty(_presence.Removed);
    }

    [Fact]
    public async Task RemovePerson_APersonWhoIsNotAMember_IsPersonNotFound_AndNothingChanges()
    {
        AddRoom(room => { room.MemberGuids = [_ann, _cat]; room.MemberConnections.Remove(_bob); });

        var result = await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.Equal(ErrorCodes.PersonNotFound, result.Errors[0].Code);
        Assert.Equal(["conn-bob"], _people.Get(_bob)!.Connections.Keys);
        Assert.Empty(_presence.Removed);
    }

    [Fact]
    public async Task RemovePerson_AnOnlineMember_LeavesTheRoomWithTheirVote()
    {
        AddRoom(room => { room.Votes[_bob] = "8"; room.Votes[_cat] = "5"; room.DepartedVotes[_bob] = "13"; });

        var result = await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.True(result.IsSuccess);
        var room = _rooms.Get("blue-fox")!;
        Assert.DoesNotContain(_bob, room.MemberGuids);
        Assert.False(room.MemberConnections.ContainsKey(_bob));
        Assert.False(room.Votes.ContainsKey(_bob));
        Assert.False(room.DepartedVotes.ContainsKey(_bob));
        Assert.Equal("5", room.Votes[_cat]);
    }

    [Fact]
    public async Task RemovePerson_AnOffgridMemberOfARestoredRoom_CanBeRemoved()
    {
        AddRoom(room =>
        {
            room.MemberConnections = [];
            room.OffgridGuids = [_ann, _bob, _cat];
            room.DroppedAt = new Dictionary<Guid, DateTimeOffset> { [_ann] = _time.GetUtcNow(), [_bob] = _time.GetUtcNow(), [_cat] = _time.GetUtcNow() };
            room.AwaitingReturn = true;
        });

        var result = await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.True(result.IsSuccess);
        var room = _rooms.Get("blue-fox")!;
        Assert.DoesNotContain(_bob, room.OffgridGuids);
        Assert.False(room.DroppedAt.ContainsKey(_bob));
    }

    [Fact]
    public async Task RemovePerson_TellsTheRoomWithTheReasonTheConnectionsAndNoOwnerChange()
    {
        AddRoom();

        await _service.RemovePersonAsync("blue-fox", _bob);

        var removed = Assert.Single(_presence.Removed);
        Assert.Equal("blue-fox", removed.RoomId);
        Assert.Equal(_bob, removed.Person.Guid);
        Assert.Equal(RoomEventReasons.RemovedByOperator, removed.Reason);
        Assert.True(removed.PersonLeft);
        Assert.False(removed.RoomDisposed);
        Assert.Null(removed.NewOwner);
        Assert.Equal(["conn-bob"], removed.ConnectionIds);
    }

    [Fact]
    public async Task RemovePerson_ForgetsThePersonsConnectionsToThatRoomOnly()
    {
        AddRoom();
        _people.Update(_bob, entity => entity.Connections["conn-elsewhere"] = "other-room", out _);

        await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.Equal(["conn-elsewhere"], _people.Get(_bob)!.Connections.Keys);
    }

    [Fact]
    public async Task RemovePerson_TheOwner_HandsTheRoomToAConnectedMember()
    {
        AddRoom(room => room.MemberConnections.Remove(_bob));

        await _service.RemovePersonAsync("blue-fox", _ann);

        var room = _rooms.Get("blue-fox")!;
        Assert.Equal(_cat, room.OwnerGuid);
        var removed = Assert.Single(_presence.Removed);
        Assert.Equal(_cat, removed.NewOwner!.Guid);
    }

    [Fact]
    public async Task RemovePerson_TheOwnerWhenNobodyElseIsConnected_HandsTheRoomToAnyMember()
    {
        AddRoom(room => room.MemberConnections = new Dictionary<Guid, HashSet<string>> { [_ann] = ["conn-ann"] });

        await _service.RemovePersonAsync("blue-fox", _ann);

        Assert.Contains(_rooms.Get("blue-fox")!.OwnerGuid, new[] { _bob, _cat });
    }

    [Fact]
    public async Task RemovePerson_TheLastMember_DisposesAndRemovesTheRoom()
    {
        AddRoom(room => { room.MemberGuids = [_bob]; room.MemberConnections = new Dictionary<Guid, HashSet<string>> { [_bob] = ["conn-bob"] }; });

        var result = await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.True(result.IsSuccess);
        Assert.Null(_rooms.Get("blue-fox"));
        Assert.True(Assert.Single(_presence.Removed).RoomDisposed);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task RemovePerson_WhenTheOthersHadAllVoted_StartsTheCountdown()
    {
        AddRoom(room =>
        {
            room.Votes[_ann] = "5";
            room.Votes[_cat] = "5";
        });

        await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.Single(_timer.Started);
        Assert.Equal(("blue-fox", 5), Assert.Single(_countdown.Ticks));
    }

    [Fact]
    public async Task RemovePerson_WhenSomeoneStillHasNotVoted_StartsNoCountdown()
    {
        AddRoom(room => room.Votes[_ann] = "5");

        await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task RemovePerson_RecordsTheActionInTheIssuesLog()
    {
        AddRoom();

        await _service.RemovePersonAsync("blue-fox", _bob);

        var entry = Assert.Single(_issues.Recorded);
        Assert.Equal(IssueLogConstants.AdminSource, entry.Source);
        Assert.Contains("removed Bob from room \"blue-fox\"", entry.Message);
        Assert.Contains("removed by operator", entry.Message);
    }

    [Fact]
    public async Task RemovePerson_TheSamePersonTwice_ThePersonIsNotFoundTheSecondTime()
    {
        AddRoom();
        await _service.RemovePersonAsync("blue-fox", _bob);

        var again = await _service.RemovePersonAsync("blue-fox", _bob);

        Assert.Equal(ErrorCodes.PersonNotFound, again.Errors[0].Code);
        Assert.Single(_presence.Removed);
    }
}
