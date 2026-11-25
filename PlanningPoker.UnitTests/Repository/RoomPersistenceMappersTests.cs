using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Repository;

public class RoomPersistenceMappersTests
{
    [Fact]
    public void RoomEntityCopyMapper_CopiesLastActivityAndAwaitingReturn()
    {
        var mapper = new RoomEntityCopyMapper();
        var activity = new DateTimeOffset(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);

        var copy = mapper.Map(new RoomEntity { Id = "r", LastActivityAt = activity, AwaitingReturn = true });

        Assert.Equal(activity, copy.LastActivityAt);
        Assert.True(copy.AwaitingReturn);
    }

    [Fact]
    public void StoredRoomMapper_StoresOwnerAndMembersWithoutConnections()
    {
        var people = new PersonRepository();
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        people.TryAdd(new PersonEntity { Guid = owner, Name = "Ann", PersonType = "dev", Connections = { ["c1"] = "r" } });
        people.TryAdd(new PersonEntity { Guid = member, Name = "Bob", PersonType = "test", CardBack = "checker" });
        var room = new RoomEntity
        {
            Id = "r",
            OwnerGuid = owner,
            MemberGuids = [member],
            MemberConnections = new Dictionary<Guid, HashSet<string>> { [member] = ["c2"] }
        };
        var savedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var stored = new StoredRoomMapper().Map(room, people, savedAt);

        Assert.Equal(["Ann", "Bob"], stored.People.Select(person => person.Name).Order());
        Assert.All(stored.People, person => Assert.Empty(person.Connections));
        Assert.Empty(stored.Room.MemberConnections);
        Assert.Equal(savedAt, stored.SavedAt);
        Assert.Equal("checker", stored.People.Single(person => person.Name == "Bob").CardBack);
        Assert.Equal("classic", stored.People.Single(person => person.Name == "Ann").CardBack);
    }

    [Fact]
    public void StoredRoomMapper_MemberWithoutAPersonRecord_IsLeftOut()
    {
        var owner = Guid.NewGuid();

        var stored = new StoredRoomMapper().Map(new RoomEntity { Id = "r", OwnerGuid = owner, MemberGuids = [owner] }, new PersonRepository(), DateTimeOffset.UnixEpoch);

        Assert.Empty(stored.People);
    }

    [Fact]
    public void StoredRoomMapper_KeepsTheSessionToken()
    {
        var people = new PersonRepository();
        var owner = Guid.NewGuid();
        people.TryAdd(new PersonEntity { Guid = owner, SessionToken = "token-ann", Name = "Ann", PersonType = "dev" });

        var stored = new StoredRoomMapper().Map(new RoomEntity { Id = "r", OwnerGuid = owner }, people, DateTimeOffset.UnixEpoch);

        Assert.Equal("token-ann", Assert.Single(stored.People).SessionToken);
    }
}
