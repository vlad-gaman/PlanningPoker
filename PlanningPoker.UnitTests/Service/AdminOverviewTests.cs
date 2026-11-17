using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class ServerStatisticsTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void StartedAt_IsWhenTheStatisticsWereCreated()
    {
        var statistics = new ServerStatistics(_time);
        _time.Advance(TimeSpan.FromHours(2));

        Assert.Equal(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero), statistics.StartedAt);
    }

    [Fact]
    public void RoomsRestoredAtStart_IsWhatWasRecorded()
    {
        var statistics = new ServerStatistics(_time);

        statistics.RecordRestored(4);

        Assert.Equal(4, statistics.RoomsRestoredAtStart);
    }

    [Fact]
    public void RoomsSweptSince_AddsTheSweepsInTheWindow()
    {
        var statistics = new ServerStatistics(_time);
        statistics.RecordSweep(2, 5);
        _time.Advance(TimeSpan.FromHours(1));
        statistics.RecordSweep(3, 0);

        Assert.Equal(5, statistics.RoomsSweptSince(_time.GetUtcNow() - TimeSpan.FromHours(24)));
        Assert.Equal(3, statistics.RoomsSweptSince(_time.GetUtcNow() - TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void RoomsSweptSince_ForgetsSweepsOlderThanADay()
    {
        var statistics = new ServerStatistics(_time);
        statistics.RecordSweep(7, 0);
        _time.Advance(TimeSpan.FromHours(25));
        statistics.RecordSweep(1, 0);

        Assert.Equal(1, statistics.RoomsSweptSince(DateTimeOffset.MinValue));
    }

    [Fact]
    public void RecordSweep_NothingRemoved_AddsNothing()
    {
        var statistics = new ServerStatistics(_time);

        statistics.RecordSweep(0, 9);

        Assert.Equal(0, statistics.RoomsSweptSince(DateTimeOffset.MinValue));
    }
}

public class PersistenceStatusTests
{
    private static readonly DateTimeOffset At = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Snapshot_BeforeAnyWrite_IsEmpty()
    {
        var snapshot = new PersistenceStatus().Snapshot();

        Assert.Null(snapshot.LastWriteAt);
        Assert.Null(snapshot.LastWriteSucceeded);
    }

    [Fact]
    public void RecordWrite_NoFailures_Succeeded()
    {
        var status = new PersistenceStatus();

        status.RecordWrite(At, 0);

        Assert.Equal(At, status.Snapshot().LastWriteAt);
        Assert.True(status.Snapshot().LastWriteSucceeded);
    }

    [Fact]
    public void RecordWrite_SomeFailures_FailedAndCountsThem()
    {
        var status = new PersistenceStatus();

        status.RecordWrite(At, 2);

        Assert.False(status.Snapshot().LastWriteSucceeded);
        Assert.Equal(2, status.Snapshot().FailedRooms);
    }

    [Fact]
    public void RecordWrite_TheLatestWins()
    {
        var status = new PersistenceStatus();
        status.RecordWrite(At, 2);

        status.RecordWrite(At.AddMinutes(1), 0);

        Assert.True(status.Snapshot().LastWriteSucceeded);
        Assert.Equal(At.AddMinutes(1), status.Snapshot().LastWriteAt);
    }
}

public class AdminRoomMapperTests
{
    private readonly AdminRoomMapper _mapper = new();
    private readonly PersonRepository _people = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();

    public AdminRoomMapperTests()
    {
        _people.TryAdd(new PersonEntity { Guid = _owner, Name = "Ann", PersonType = "dev" });
        _people.TryAdd(new PersonEntity { Guid = _member, Name = "Bob", PersonType = "obs" });
    }

    private RoomEntity Room(Action<RoomEntity>? change = null)
    {
        var room = new RoomEntity
        {
            Id = "blue-fox",
            OwnerGuid = _owner,
            MemberGuids = [_owner, _member],
            VotingEnabled = true,
            CreatedAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero),
            LastActivityAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero)
        };
        change?.Invoke(room);
        return room;
    }

    [Fact]
    public void Map_CarriesTheRoomAndItsPeople()
    {
        var result = _mapper.Map(Room(), _people);

        Assert.Equal("blue-fox", result.Id);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), result.CreatedAt);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), result.LastActivityAt);
        Assert.Equal(["Ann", "Bob"], result.People.Select(person => person.Name));
        Assert.Equal(["dev", "obs"], result.People.Select(person => person.PersonType));
    }

    [Fact]
    public void Map_MarksTheOwner()
    {
        var result = _mapper.Map(Room(), _people);

        Assert.True(result.People.Single(person => person.Id == _owner).IsOwner);
        Assert.False(result.People.Single(person => person.Id == _member).IsOwner);
    }

    [Fact]
    public void Map_AMemberAnnouncedOffgrid_IsOffgrid_AndTheOthersAreOnline()
    {
        var dropped = new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);
        var result = _mapper.Map(Room(room =>
        {
            room.OffgridGuids = [_member];
            room.DroppedAt[_member] = dropped;
        }), _people);

        var bob = result.People.Single(person => person.Id == _member);
        Assert.Equal(AdminPersonStatus.Offgrid, bob.Status);
        Assert.Equal(dropped, bob.LastSeenAt);
        var ann = result.People.Single(person => person.Id == _owner);
        Assert.Equal(AdminPersonStatus.Online, ann.Status);
        Assert.Null(ann.LastSeenAt);
    }

    [Fact]
    public void Map_VotingOpen_IsVoting()
    {
        Assert.Equal(AdminRoomState.Voting, _mapper.Map(Room(), _people).State);
    }

    [Fact]
    public void Map_VotesRevealed_IsRevealed()
    {
        Assert.Equal(AdminRoomState.Revealed, _mapper.Map(Room(room => room.VotingEnabled = false), _people).State);
    }

    [Fact]
    public void Map_AwaitingReturn_IsRestoredWhateverTheVoting()
    {
        Assert.Equal(AdminRoomState.Restored, _mapper.Map(Room(room => room.AwaitingReturn = true), _people).State);
        Assert.Equal(AdminRoomState.Restored, _mapper.Map(Room(room => { room.AwaitingReturn = true; room.VotingEnabled = false; }), _people).State);
    }

    [Fact]
    public void Map_ACreatorWhoNeverJoined_IsNotListed()
    {
        var result = _mapper.Map(Room(room => room.MemberGuids = [_member]), _people);

        Assert.Equal(["Bob"], result.People.Select(person => person.Name));
    }

    [Fact]
    public void Map_AMemberWithNoPerson_IsSkipped()
    {
        var result = _mapper.Map(Room(room => room.MemberGuids = [_owner, Guid.NewGuid()]), _people);

        Assert.Equal(["Ann"], result.People.Select(person => person.Name));
    }

    [Fact]
    public void Map_IgnoresVotesEntirely()
    {
        var result = _mapper.Map(Room(room =>
        {
            room.Votes[_owner] = "SECRET-13";
            room.DepartedVotes[_member] = "SECRET-8";
        }), _people);

        Assert.DoesNotContain("SECRET", System.Text.Json.JsonSerializer.Serialize(result));
    }
}

public class AdminOverviewMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static AdminPerson Person(AdminPersonStatus status)
    {
        return new AdminPerson { Id = Guid.NewGuid(), Name = "P", PersonType = "dev", Status = status };
    }

    private static AdminRoom Room(AdminRoomState state, params AdminPerson[] people)
    {
        return new AdminRoom { Id = Guid.NewGuid().ToString("N"), State = state, People = people };
    }

    private static AdminOverview Map(IReadOnlyList<AdminRoom> rooms, PersistenceStatusSnapshot? write = null)
    {
        return new AdminOverviewMapper().Map(rooms, [], TimeSpan.FromHours(3), 2, 6, write ?? new PersistenceStatusSnapshot(), 9);
    }

    [Fact]
    public void Map_CountsRoomsByState()
    {
        var overview = Map([
            Room(AdminRoomState.Voting), Room(AdminRoomState.Voting), Room(AdminRoomState.Revealed), Room(AdminRoomState.Restored)]);

        Assert.Equal(4, overview.Stats.Rooms);
        Assert.Equal(2, overview.Stats.VotingRooms);
        Assert.Equal(1, overview.Stats.RevealedRooms);
        Assert.Equal(1, overview.Stats.RestoredRooms);
    }

    [Fact]
    public void Map_CountsPeopleOnlineAndOffgridAcrossRooms()
    {
        var overview = Map([
            Room(AdminRoomState.Voting, Person(AdminPersonStatus.Online), Person(AdminPersonStatus.Offgrid)),
            Room(AdminRoomState.Restored, Person(AdminPersonStatus.Offgrid), Person(AdminPersonStatus.Offgrid), Person(AdminPersonStatus.Online))]);

        Assert.Equal(2, overview.Stats.PeopleOnline);
        Assert.Equal(3, overview.Stats.PeopleOffgrid);
    }

    [Fact]
    public void Map_CarriesTheServerFigures()
    {
        var overview = Map([], new PersistenceStatusSnapshot { LastWriteAt = Now, LastWriteSucceeded = false, FailedRooms = 2 });

        Assert.Equal(TimeSpan.FromHours(3), overview.Stats.Uptime);
        Assert.Equal(2, overview.Stats.RoomsRestoredAtStart);
        Assert.Equal(6, overview.Stats.AbandonedRoomsSweptLast24Hours);
        Assert.Equal(9, overview.Stats.FailedLoginsLast24Hours);
        Assert.Equal(Now, overview.Stats.LastStoreWrite.At);
        Assert.False(overview.Stats.LastStoreWrite.Succeeded);
        Assert.Equal(2, overview.Stats.LastStoreWrite.FailedRooms);
    }

    [Fact]
    public void Map_NoRooms_GivesZeroes()
    {
        var overview = Map([]);

        Assert.Equal(0, overview.Stats.Rooms);
        Assert.Equal(0, overview.Stats.PeopleOnline);
        Assert.Null(overview.Stats.LastStoreWrite.At);
        Assert.Null(overview.Stats.LastStoreWrite.Succeeded);
    }
}

public class AdminOverviewServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly PersonRepository _people = new();
    private readonly FakeIssueStore _store = new();
    private readonly PersistenceStatus _persistence = new();
    private readonly RoomRepository _rooms;
    private readonly ServerStatistics _server;
    private readonly AdminOverviewService _service;

    public AdminOverviewServiceTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()), time: _time);
        _server = new ServerStatistics(_time);
        var options = Options.Create(new IssueLogOptions());
        var log = new IssueLog(_store, new IssueRedactor(options), new IssueEntryMapper(), options, _time, NullLogger<IssueLog>.Instance);
        _service = new AdminOverviewService(_rooms, _people, _server, _persistence, log, new AdminRoomMapper(), new AdminOverviewMapper(), _time);
    }

    private void AddRoom(string id, DateTimeOffset lastActivity, string name = "Ann", bool awaiting = false)
    {
        var owner = Guid.NewGuid();
        _people.TryAdd(new PersonEntity { Guid = owner, Name = name, PersonType = "dev" });
        _rooms.TryAdd(new RoomEntity
        {
            Id = id,
            OwnerGuid = owner,
            MemberGuids = [owner],
            VotingEnabled = true,
            AwaitingReturn = awaiting,
            CreatedAt = lastActivity,
            LastActivityAt = lastActivity
        });
    }

    [Fact]
    public void GetOverview_ListsTheLiveRoomsNewestActivityFirst()
    {
        AddRoom("old", _time.GetUtcNow().AddHours(-3));
        AddRoom("new", _time.GetUtcNow());
        AddRoom("mid", _time.GetUtcNow().AddHours(-1));

        var overview = _service.GetOverview().Data!;

        Assert.Equal(["new", "mid", "old"], overview.Rooms.Select(room => room.Id));
        Assert.Equal(3, overview.Stats.Rooms);
    }

    [Fact]
    public void GetOverview_UptimeRunsFromTheServerStart()
    {
        _time.Advance(TimeSpan.FromMinutes(90));

        Assert.Equal(TimeSpan.FromMinutes(90), _service.GetOverview().Data!.Stats.Uptime);
    }

    [Fact]
    public void GetOverview_ReportsRestoredAndSweptRooms()
    {
        _server.RecordRestored(3);
        _server.RecordSweep(2, 1);
        AddRoom("back", _time.GetUtcNow(), awaiting: true);

        var stats = _service.GetOverview().Data!.Stats;

        Assert.Equal(3, stats.RoomsRestoredAtStart);
        Assert.Equal(2, stats.AbandonedRoomsSweptLast24Hours);
        Assert.Equal(1, stats.RestoredRooms);
    }

    [Fact]
    public void GetOverview_ReportsTheLastStoreWrite()
    {
        _persistence.RecordWrite(_time.GetUtcNow(), 1);

        var write = _service.GetOverview().Data!.Stats.LastStoreWrite;

        Assert.Equal(_time.GetUtcNow(), write.At);
        Assert.False(write.Succeeded);
        Assert.Equal(1, write.FailedRooms);
    }

    [Fact]
    public void GetOverview_CountsOnlyFailedLoginsFromTheLastDay()
    {
        _store.Entries.Add(new IssueEntry { Time = _time.GetUtcNow().AddHours(-30), Level = IssueLevel.Warning, Source = "Admin", Message = "old", IpAddress = "203.0.113.1" });
        _store.Entries.Add(new IssueEntry { Time = _time.GetUtcNow().AddHours(-2), Level = IssueLevel.Warning, Source = "Admin", Message = "recent", IpAddress = "203.0.113.2" });
        _store.Entries.Add(new IssueEntry { Time = _time.GetUtcNow().AddHours(-1), Level = IssueLevel.Info, Source = "Admin", Message = "signed in" });

        Assert.Equal(1, _service.GetOverview().Data!.Stats.FailedLoginsLast24Hours);
    }

    [Fact]
    public void GetOverview_CarriesTheNewestIssuesAtMostFifty()
    {
        for (var i = 0; i < 60; i++)
        {
            _store.Add(new IssueEntry { Time = _time.GetUtcNow(), Level = IssueLevel.Info, Source = "Test", Message = $"entry {i}" });
        }

        var issues = _service.GetOverview().Data!.Issues;

        Assert.Equal(AdminOverviewService.IssueCount, issues.Count);
        Assert.Equal("entry 59", issues[0].Message);
    }

    [Fact]
    public void GetOverview_NothingYet_IsEmptyAndSucceeds()
    {
        var response = _service.GetOverview();

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Data!.Rooms);
        Assert.Empty(response.Data.Issues);
    }
}

public class AdminOverviewResponseMapperTests
{
    private static AdminOverview Overview()
    {
        var person = new AdminPerson { Id = Guid.NewGuid(), Name = "Ann", PersonType = "dev", IsOwner = true, Status = AdminPersonStatus.Offgrid, LastSeenAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero) };
        return new AdminOverview
        {
            Stats = new AdminStats
            {
                Rooms = 1,
                VotingRooms = 1,
                PeopleOffgrid = 1,
                Uptime = TimeSpan.FromSeconds(125.9),
                RoomsRestoredAtStart = 2,
                AbandonedRoomsSweptLast24Hours = 3,
                FailedLoginsLast24Hours = 4,
                LastStoreWrite = new AdminStoreWrite { At = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), Succeeded = true }
            },
            Rooms = [new AdminRoom { Id = "blue-fox", State = AdminRoomState.Restored, People = [person] }],
            Issues = [new IssueEntry { Id = 7, Time = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), Level = IssueLevel.Error, Source = "Sweep", Message = "boom", IpAddress = "203.0.113.9" }]
        };
    }

    [Fact]
    public void Map_StatsAreCopiedAcross()
    {
        var response = new AdminOverviewResponseMapper().Map(Overview());

        Assert.Equal(1, response.Stats.Rooms.Total);
        Assert.Equal(1, response.Stats.Rooms.Voting);
        Assert.Equal(1, response.Stats.People.Offgrid);
        Assert.Equal(125, response.Stats.UptimeSeconds);
        Assert.Equal(2, response.Stats.RoomsRestoredAtStart);
        Assert.Equal(3, response.Stats.AbandonedRoomsSweptLast24Hours);
        Assert.Equal(4, response.Stats.FailedLoginsLast24Hours);
        Assert.True(response.Stats.LastStoreWrite.Succeeded);
    }

    [Fact]
    public void Map_RoomsAndPeopleUseLowercaseNames()
    {
        var response = new AdminOverviewResponseMapper().Map(Overview());

        var room = Assert.Single(response.Rooms);
        Assert.Equal("restored", room.State);
        var person = Assert.Single(room.People);
        Assert.Equal("offgrid", person.Status);
        Assert.True(person.IsOwner);
    }

    [Fact]
    public void Map_IssuesDropTheClientAddress()
    {
        var response = new AdminOverviewResponseMapper().Map(Overview());

        var issue = Assert.Single(response.Issues);
        Assert.Equal("error", issue.Level);
        Assert.DoesNotContain("203.0.113.9", System.Text.Json.JsonSerializer.Serialize(response));
    }

    [Theory]
    [InlineData(typeof(AdminRoom))]
    [InlineData(typeof(AdminPerson))]
    [InlineData(typeof(AdminStats))]
    [InlineData(typeof(AdminOverviewResponse))]
    [InlineData(typeof(AdminRoomResponse))]
    [InlineData(typeof(AdminPersonResponse))]
    [InlineData(typeof(AdminStatsResponse))]
    public void NoOverviewModelHasAVoteMember(Type type)
    {
        var names = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name);

        Assert.DoesNotContain(names, name => name.Contains("vote", StringComparison.OrdinalIgnoreCase) || name.Contains("card", StringComparison.OrdinalIgnoreCase));
    }
}
