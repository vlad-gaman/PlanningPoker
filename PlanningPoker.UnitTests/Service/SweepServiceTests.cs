using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class SweepServiceTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms;

    public SweepServiceTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()), time: _time);
    }

    private SweepService CreateService(IRoomRepository? rooms = null, IPersonRepository? people = null)
    {
        return new SweepService(rooms ?? _rooms, people ?? _people, Options.Create(new SweepOptions { AbandonedAfter = Ttl }), _time);
    }

    private Guid AddPerson(DateTimeOffset? lastSeenAt = null, params string[] connections)
    {
        var person = new PersonEntity
        {
            Guid = Guid.NewGuid(),
            Name = "Ann",
            PersonType = "dev",
            LastSeenAt = lastSeenAt ?? _time.GetUtcNow(),
            Connections = connections.ToDictionary(connection => connection, _ => "room")
        };
        _people.TryAdd(person);
        return person.Guid;
    }

    private string AddRoom(Guid owner, DateTimeOffset? createdAt = null, Guid? member = null)
    {
        var room = new RoomEntity { Id = Guid.NewGuid().ToString(), OwnerGuid = owner, CreatedAt = createdAt ?? _time.GetUtcNow() };
        if (member is { } memberGuid)
        {
            room.MemberGuids = [memberGuid];
            room.MemberConnections = new Dictionary<Guid, HashSet<string>> { [memberGuid] = ["c1"] };
        }

        _rooms.TryAdd(room);
        return room.Id;
    }

    private DateTimeOffset Ago(TimeSpan span) => _time.GetUtcNow() - span;

    [Fact]
    public void Sweep_NothingStored_RemovesNothing()
    {
        var result = CreateService().Sweep();

        Assert.Equal(0, result.Rooms);
        Assert.Equal(0, result.People);
    }

    [Fact]
    public void Sweep_RoomWithoutMembersOlderThanTtl_IsRemoved()
    {
        var roomId = AddRoom(Guid.NewGuid(), Ago(Ttl + TimeSpan.FromSeconds(1)));

        var result = CreateService().Sweep();

        Assert.Equal(1, result.Rooms);
        Assert.Null(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_RoomCreatedExactlyTtlAgo_IsKept()
    {
        var roomId = AddRoom(Guid.NewGuid(), Ago(Ttl));

        var result = CreateService().Sweep();

        Assert.Equal(0, result.Rooms);
        Assert.NotNull(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_RoomCreatedJustInsideTtl_IsKept()
    {
        var roomId = AddRoom(Guid.NewGuid(), Ago(Ttl - TimeSpan.FromSeconds(1)));

        CreateService().Sweep();

        Assert.NotNull(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_OldRoomWithAConnectedMember_IsKept()
    {
        var owner = AddPerson(Ago(Ttl * 2), "c1");
        var roomId = AddRoom(owner, Ago(Ttl * 2), owner);

        var result = CreateService().Sweep();

        Assert.Equal(0, result.Rooms);
        Assert.NotNull(_rooms.Get(roomId));
        Assert.NotNull(_people.Get(owner));
    }

    [Fact]
    public void Sweep_ClockAdvancesPastTtl_RemovesWhatWasKeptBefore()
    {
        var roomId = AddRoom(Guid.NewGuid());
        var service = CreateService();
        service.Sweep();
        Assert.NotNull(_rooms.Get(roomId));

        _time.Advance(Ttl + TimeSpan.FromSeconds(1));
        var result = service.Sweep();

        Assert.Equal(1, result.Rooms);
        Assert.Null(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_PersonUnconnectedUnreferencedAndStale_IsRemoved()
    {
        var guid = AddPerson(Ago(Ttl + TimeSpan.FromSeconds(1)));

        var result = CreateService().Sweep();

        Assert.Equal(1, result.People);
        Assert.Null(_people.Get(guid));
    }

    [Fact]
    public void Sweep_PersonLastSeenExactlyTtlAgo_IsKept()
    {
        var guid = AddPerson(Ago(Ttl));

        var result = CreateService().Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(guid));
    }

    [Fact]
    public void Sweep_PersonSeenRecently_IsKept()
    {
        var guid = AddPerson(Ago(TimeSpan.FromHours(1)));

        CreateService().Sweep();

        Assert.NotNull(_people.Get(guid));
    }

    [Fact]
    public void Sweep_StalePersonWithAnOpenConnection_IsKept()
    {
        var guid = AddPerson(Ago(Ttl * 2), "c1");

        var result = CreateService().Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(guid));
    }

    [Fact]
    public void Sweep_StalePersonWhoOwnsASurvivingRoom_IsKept()
    {
        var owner = AddPerson(Ago(Ttl * 2));
        AddRoom(owner);

        var result = CreateService().Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(owner));
    }

    [Fact]
    public void Sweep_StalePersonListedAsAMemberOfASurvivingRoom_IsKept()
    {
        var member = AddPerson(Ago(Ttl * 2));
        var room = new RoomEntity { Id = "room", OwnerGuid = Guid.NewGuid(), MemberGuids = [member], CreatedAt = _time.GetUtcNow() };
        _rooms.TryAdd(room);

        var result = CreateService().Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(member));
    }

    [Fact]
    public void Sweep_RoomAndItsCreatorBothAbandoned_RemovesBothInOneRun()
    {
        var creator = AddPerson(Ago(Ttl + TimeSpan.FromSeconds(1)));
        var roomId = AddRoom(creator, Ago(Ttl + TimeSpan.FromSeconds(1)));

        var result = CreateService().Sweep();

        Assert.Equal(1, result.Rooms);
        Assert.Equal(1, result.People);
        Assert.Null(_rooms.Get(roomId));
        Assert.Null(_people.Get(creator));
    }

    [Fact]
    public void Sweep_CountsEveryRemovedRoomAndPerson()
    {
        for (var i = 0; i < 3; i++)
        {
            AddRoom(Guid.NewGuid(), Ago(Ttl * 2));
            AddPerson(Ago(Ttl * 2));
        }

        var result = CreateService().Sweep();

        Assert.Equal(3, result.Rooms);
        Assert.Equal(3, result.People);
        Assert.Empty(_rooms.Ids());
        Assert.Empty(_people.Guids());
    }

    [Fact]
    public void Sweep_UsesConfiguredAbandonedAfter()
    {
        var roomId = AddRoom(Guid.NewGuid(), Ago(TimeSpan.FromMinutes(5)));
        var service = new SweepService(_rooms, _people, Options.Create(new SweepOptions { AbandonedAfter = TimeSpan.FromMinutes(1) }), _time);

        service.Sweep();

        Assert.Null(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_JoinArrivesRightAfterTheRoomIsMarkedAbandoned_JoinIsRefusedAndRoomIsRemoved()
    {
        var roomId = AddRoom(Guid.NewGuid(), Ago(Ttl * 2));
        var interleaving = new InterleavingRoomRepository(_rooms);
        var joiner = AddPerson();
        var joinWasRefused = false;
        interleaving.AfterNextUpdate = () =>
        {
            var join = RoomServiceFactory.Create(_rooms, _people, null, _time).Join(joiner, "c-late", roomId);
            joinWasRefused = !join.IsSuccess;
        };

        var result = CreateService(interleaving).Sweep();

        Assert.Equal(1, result.Rooms);
        Assert.True(joinWasRefused);
        Assert.Null(_rooms.Get(roomId));
    }

    [Fact]
    public void Sweep_PersonGainsAConnectionAfterTheSnapshot_IsKept()
    {
        var guid = AddPerson(Ago(Ttl * 2));
        var interleaving = new InterleavingPersonRepository(_people);
        interleaving.BeforeNextRemoveIf = () => _people.Update(guid, person => person.Connections["c-late"] = "room", out _);

        var result = CreateService(people: interleaving).Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(guid));
    }

    [Fact]
    public void Sweep_PersonIsUpsertedAfterTheSnapshot_IsKept()
    {
        var guid = AddPerson(Ago(Ttl * 2));
        var interleaving = new InterleavingPersonRepository(_people);
        interleaving.BeforeNextRemoveIf = () => _people.Update(guid, person => person.LastSeenAt = _time.GetUtcNow(), out _);

        var result = CreateService(people: interleaving).Sweep();

        Assert.Equal(0, result.People);
        Assert.NotNull(_people.Get(guid));
    }

    [Fact]
    public void Sweep_PersonRemovedBySweep_RejectsALaterUpdateOnTheStaleEntity()
    {
        var guid = AddPerson(Ago(Ttl * 2));
        var stale = _people.Get(guid)!;

        CreateService().Sweep();

        Assert.True(stale.IsRemoved);
        Assert.False(_people.Update(guid, _ => 1, out _));
    }

    [Fact]
    public void SweepConstants_DefaultToADayAndTenMinutes()
    {
        var options = new SweepOptions();

        Assert.Equal(TimeSpan.FromHours(24), options.AbandonedAfter);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Interval);
        Assert.Equal("Sweep", SweepConstants.ConfigurationSection);
    }
}
