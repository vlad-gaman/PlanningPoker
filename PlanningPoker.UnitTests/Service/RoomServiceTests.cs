using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomServiceTests
{
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper()));

    private static RoomSettings Settings(bool fun = false)
    {
        return new RoomSettings
        {
            UseFunRoomName = fun,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 7, ShowFireworks = false, MaxParticipants = 9 }
        };
    }

    private RoomService CreateService(IRoomNameGenerator? generator = null, IPersonRepository? people = null)
    {
        return new RoomService(
            _rooms,
            people ?? _people,
            generator ?? new StubRoomNameGenerator("unused"),
            new RoomEntityMapper(new RoomConfigurationEntityMapper()),
            new RoomMapper(new RoomConfigurationMapper()),
            new PersonMapper(),
            new VoteStatusMapper(),
            new JoinRoomResultMapper(),
            new ExitRoomResultMapper(),
            new RoomIdentifierValidation(),
            new RoomExistsValidation(_rooms));
    }

    private Guid AddPerson(string name = "Ann", string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = name, PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    private string NewRoom(Guid owner)
    {
        return CreateService().Create(owner, Settings()).Data!.Id;
    }

    private IReadOnlyDictionary<string, string> ConnectionsOf(Guid person)
    {
        return _people.Get(person)!.Connections;
    }

    [Fact]
    public void Create_WithoutFunName_UsesGuidIdAndMakesCreatorOwnerAndFirstMember()
    {
        var owner = AddPerson();

        var response = CreateService().Create(owner, Settings());

        Assert.True(response.IsSuccess);
        Assert.True(Guid.TryParse(response.Data!.Id, out _));
        var stored = _rooms.Get(response.Data.Id)!;
        Assert.Equal(owner, stored.OwnerGuid);
        Assert.Equal([owner], stored.MemberGuids);
        Assert.Equal("linear", stored.Configuration.CardSet);
        Assert.Equal(7, stored.Configuration.CountdownSeconds);
    }

    [Fact]
    public void Create_WithFunName_UsesGeneratedId()
    {
        var response = CreateService(new StubRoomNameGenerator("42_big_red_cat")).Create(AddPerson(), Settings(fun: true));

        Assert.Equal("42_big_red_cat", response.Data!.Id);
    }

    [Fact]
    public void Create_IdCollision_RetriesWithNextId()
    {
        _rooms.TryAdd(new RoomEntity { Id = "taken" });
        var generator = new StubRoomNameGenerator("taken", "taken", "free");

        var response = CreateService(generator).Create(AddPerson(), Settings(fun: true));

        Assert.Equal("free", response.Data!.Id);
        Assert.Equal(3, generator.Calls);
    }

    [Fact]
    public void Create_EveryAttemptCollides_FailsAfterFiveAttempts()
    {
        _rooms.TryAdd(new RoomEntity { Id = "taken" });
        var generator = new StubRoomNameGenerator("taken");

        var response = CreateService(generator).Create(AddPerson(), Settings(fun: true));

        Assert.Equal(ErrorCodes.RoomIdGenerationFailed, Assert.Single(response.Errors).Code);
        Assert.Equal(5, generator.Calls);
    }

    [Fact]
    public void Exists_KnownRoom_ReturnsId()
    {
        var room = CreateService().Create(AddPerson(), Settings()).Data!;

        Assert.Equal(room.Id, CreateService().Exists(room.Id).Data);
    }

    [Fact]
    public void Exists_UnknownRoom_ReturnsRoomNotFound()
    {
        var response = CreateService().Exists("nope");

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(response.Errors).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    public void Join_InvalidIdentifier_ReturnsValidationFailed(string roomId)
    {
        var response = CreateService().Join(AddPerson(), "c1", roomId);

        Assert.Equal(ErrorCodes.ValidationFailed, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_UnknownRoom_ReturnsRoomNotFound()
    {
        var response = CreateService().Join(AddPerson(), "c1", "missing");

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_UnknownPerson_ReturnsNoSession()
    {
        var room = CreateService().Create(AddPerson(), Settings()).Data!;

        var response = CreateService().Join(Guid.NewGuid(), "c1", room.Id);

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_AddsMemberRegistersConnectionAndReturnsEveryone()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest", PersonTypes.Tester);
        var room = CreateService().Create(owner, Settings()).Data!;

        var response = CreateService().Join(guest, "conn-guest", room.Id);

        Assert.True(response.IsSuccess);
        Assert.Equal(["Owner", "Guest"], response.Data!.People.Select(person => person.Name));
        Assert.Equal(owner, response.Data.Room.OwnerGuid);
        Assert.Equal(guest, response.Data.Person.Guid);
        Assert.False(response.Data.VoteStatus.HasEveryoneVoted);
        Assert.Empty(response.Data.VoteStatus.Votes);
        Assert.Equal(room.Id, Assert.Single(ConnectionsOf(guest), pair => pair.Key == "conn-guest").Value);
        Assert.Equal(["conn-guest"], _rooms.Get(room.Id)!.MemberConnections[guest]);
    }

    [Fact]
    public void Join_AlreadyMember_DoesNotDuplicateMemberButRegistersTheNewConnection()
    {
        var owner = AddPerson();
        var room = CreateService().Create(owner, Settings()).Data!;

        CreateService().Join(owner, "c1", room.Id);
        var response = CreateService().Join(owner, "c2", room.Id);

        Assert.Single(response.Data!.People);
        Assert.Equal(["c1", "c2"], ConnectionsOf(owner).Keys.Order());
        Assert.Equal(["c1", "c2"], _rooms.Get(room.Id)!.MemberConnections[owner].Order());
    }

    [Fact]
    public void Join_SameConnectionSameRoomAgain_IsIdempotent()
    {
        var owner = AddPerson();
        var room = CreateService().Create(owner, Settings()).Data!;
        CreateService().Join(owner, "c1", room.Id);

        var again = CreateService().Join(owner, "c1", room.Id);

        Assert.True(again.IsSuccess);
        Assert.Single(_rooms.Get(room.Id)!.MemberGuids);
        Assert.Equal(["c1"], _rooms.Get(room.Id)!.MemberConnections[owner]);
    }

    [Fact]
    public void Join_SameConnectionAnotherRoom_IsRejectedAndLeavesTheOtherRoomAlone()
    {
        var owner = AddPerson("Owner");
        var first = NewRoom(owner);
        var second = NewRoom(AddPerson("Other"));
        CreateService().Join(owner, "c1", first);

        var response = CreateService().Join(owner, "c1", second);

        var error = Assert.Single(response.Errors);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("already_in_room", error.Metadata["reason"]);
        Assert.DoesNotContain(owner, _rooms.Get(second)!.MemberGuids);
        Assert.Equal(first, Assert.Single(ConnectionsOf(owner)).Value);
    }

    [Theory]
    [InlineData(true, PersonTypes.Developer, true)]
    [InlineData(true, PersonTypes.Observer, false)]
    [InlineData(false, PersonTypes.Developer, false)]
    public void Join_ResetsCountdownOnlyWhenVotingIsOnAndPersonIsNotObserver(bool votingEnabled, string type, bool expected)
    {
        var room = CreateService().Create(AddPerson(), Settings()).Data!;
        _rooms.Update(room.Id, entity => entity.VotingEnabled = votingEnabled, out _);

        var response = CreateService().Join(AddPerson("Guest", type), "c1", room.Id);

        Assert.Equal(expected, response.Data!.ResetCountdown);
    }

    [Fact]
    public void Join_DisposedRoom_FailsAndDropsTheConnectionAgain()
    {
        var owner = AddPerson();
        var room = NewRoom(owner);
        _rooms.Update(room, entity => entity.IsDisposed = true, out _);
        var guest = AddPerson("Guest");

        var response = CreateService().Join(guest, "c-guest", room);

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(response.Errors).Code);
        Assert.Empty(ConnectionsOf(guest));
        Assert.DoesNotContain(guest, _rooms.Get(room)!.MemberGuids);
    }

    [Fact]
    public void Exit_UnknownConnection_ReturnsNotInRoomAndKeepsTheMember()
    {
        var owner = AddPerson();
        var room = CreateService().Create(owner, Settings()).Data!;
        CreateService().Join(owner, "new-connection", room.Id);

        var response = CreateService().Exit(owner, "old-connection");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(response.Errors).Code);
        Assert.Contains(owner, _rooms.Get(room.Id)!.MemberGuids);
    }

    [Fact]
    public void Exit_PersonWithNoConnections_ReturnsNotInRoom()
    {
        var response = CreateService().Exit(AddPerson(), "c1");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Exit_UnknownPerson_ReturnsNoSession()
    {
        var response = CreateService().Exit(Guid.NewGuid(), "c1");

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Exit_RegularMember_LeavesOwnerUnchanged()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var room = CreateService().Create(owner, Settings()).Data!;
        CreateService().Join(owner, "c-owner", room.Id);
        CreateService().Join(guest, "c-guest", room.Id);

        var response = CreateService().Exit(guest, "c-guest");

        Assert.True(response.IsSuccess);
        Assert.True(response.Data!.PersonLeft);
        Assert.False(response.Data.RoomDisposed);
        Assert.Null(response.Data.NewOwner);
        Assert.Equal("Guest", response.Data.Person.Name);
        Assert.Equal([owner], _rooms.Get(room.Id)!.MemberGuids);
        Assert.Empty(ConnectionsOf(guest));
    }

    [Fact]
    public void Exit_Owner_TransfersOwnershipToAnotherMember()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var room = CreateService().Create(owner, Settings()).Data!;
        CreateService().Join(owner, "c-owner", room.Id);
        CreateService().Join(guest, "c-guest", room.Id);

        var response = CreateService().Exit(owner, "c-owner");

        Assert.False(response.Data!.RoomDisposed);
        Assert.Equal(guest, response.Data.NewOwner!.Guid);
        Assert.Equal(guest, _rooms.Get(room.Id)!.OwnerGuid);
    }

    [Fact]
    public void Exit_LastMember_DisposesRoom()
    {
        var owner = AddPerson();
        var room = CreateService().Create(owner, Settings()).Data!;
        CreateService().Join(owner, "c1", room.Id);

        var response = CreateService().Exit(owner, "c1");

        Assert.True(response.Data!.RoomDisposed);
        Assert.Null(response.Data.NewOwner);
        Assert.Equal(room.Id, response.Data.RoomId);
        Assert.Null(_rooms.Get(room.Id));
    }

    [Fact]
    public void Exit_TwoTabsInSameRoom_FirstTabKeepsPersonAndSecondRemovesThem()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var roomId = NewRoom(owner);
        CreateService().Join(owner, "owner-conn", roomId);
        CreateService().Join(guest, "tab-1", roomId);
        CreateService().Join(guest, "tab-2", roomId);

        var firstTab = CreateService().Exit(guest, "tab-1");

        Assert.True(firstTab.IsSuccess);
        Assert.False(firstTab.Data!.PersonLeft);
        Assert.False(firstTab.Data.RoomDisposed);
        Assert.Null(firstTab.Data.NewOwner);
        Assert.Contains(guest, _rooms.Get(roomId)!.MemberGuids);
        Assert.Equal(["tab-2"], ConnectionsOf(guest).Keys);

        var secondTab = CreateService().Exit(guest, "tab-2");

        Assert.True(secondTab.Data!.PersonLeft);
        Assert.DoesNotContain(guest, _rooms.Get(roomId)!.MemberGuids);
        Assert.Empty(ConnectionsOf(guest));
    }

    [Fact]
    public void Exit_TwoTabsInDifferentRooms_LeavesOnlyTheRoomOfThatTab()
    {
        var guest = AddPerson("Guest");
        var firstRoom = NewRoom(AddPerson("Owner A"));
        var secondRoom = NewRoom(AddPerson("Owner B"));
        CreateService().Join(guest, "tab-1", firstRoom);
        CreateService().Join(guest, "tab-2", secondRoom);

        var response = CreateService().Exit(guest, "tab-1");

        Assert.True(response.Data!.PersonLeft);
        Assert.Equal(firstRoom, response.Data.RoomId);
        Assert.DoesNotContain(guest, _rooms.Get(firstRoom)!.MemberGuids);
        Assert.Contains(guest, _rooms.Get(secondRoom)!.MemberGuids);
        Assert.Equal(secondRoom, Assert.Single(ConnectionsOf(guest)).Value);
    }

    [Fact]
    public void Exit_OldConnectionAfterReconnect_DoesNotWipeTheNewConnection()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var roomId = NewRoom(owner);
        CreateService().Join(owner, "owner-conn", roomId);
        CreateService().Join(guest, "old-conn", roomId);
        CreateService().Join(guest, "new-conn", roomId);

        var stale = CreateService().Exit(guest, "old-conn");

        Assert.True(stale.IsSuccess);
        Assert.False(stale.Data!.PersonLeft);
        Assert.Contains(guest, _rooms.Get(roomId)!.MemberGuids);
        Assert.Equal(["new-conn"], ConnectionsOf(guest).Keys);
        Assert.Equal(["new-conn"], _rooms.Get(roomId)!.MemberConnections[guest]);
    }

    [Fact]
    public void Exit_OtherPersonJoinsDuringLastMemberExit_RoomSurvivesWithBothMembers()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var roomId = NewRoom(owner);
        CreateService().Join(owner, "owner-conn", roomId);
        var interleaving = new InterleavingPersonRepository(_people);
        ServiceResponse<JoinRoomResult>? joined = null;
        // Runs between Exit's person step and its room step: the guest is admitted while the owner is still a member.
        interleaving.AfterNextUpdate = () => joined = CreateService().Join(guest, "guest-conn", roomId);

        var exit = CreateService(people: interleaving).Exit(owner, "owner-conn");

        Assert.True(joined!.IsSuccess);
        Assert.True(exit.IsSuccess);
        Assert.False(exit.Data!.RoomDisposed);
        Assert.Equal(guest, exit.Data.NewOwner!.Guid);
        var stored = _rooms.Get(roomId)!;
        Assert.Equal([guest], stored.MemberGuids);
        Assert.Equal(guest, stored.OwnerGuid);
    }

    [Fact]
    public void Join_AfterLastMemberExit_FailsAndRoomIsGone()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var roomId = NewRoom(owner);
        CreateService().Join(owner, "owner-conn", roomId);
        CreateService().Exit(owner, "owner-conn");

        var joined = CreateService().Join(guest, "guest-conn", roomId);

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(joined.Errors).Code);
        Assert.Null(_rooms.Get(roomId));
        Assert.Empty(ConnectionsOf(guest));
    }

    [Fact]
    public void Exit_RacingSecondTabJoin_PersonStaysAndRoomSurvives()
    {
        // Regression for the same-person race: tab 1 leaves while tab 2 joins. Membership is decided under the room lock,
        // so tab 2's admission must win over tab 1's removal and the sole member must not be dropped.
        var owner = AddPerson("Owner");
        var roomId = NewRoom(owner);
        CreateService().Join(owner, "tab-1", roomId);
        var interleaving = new InterleavingPersonRepository(_people);
        ServiceResponse<JoinRoomResult>? secondTab = null;
        interleaving.AfterNextUpdate = () => secondTab = CreateService().Join(owner, "tab-2", roomId);

        var firstTab = CreateService(people: interleaving).Exit(owner, "tab-1");

        Assert.True(secondTab!.IsSuccess);
        Assert.True(firstTab.IsSuccess);
        Assert.False(firstTab.Data!.PersonLeft);
        Assert.False(firstTab.Data.RoomDisposed);
        var stored = _rooms.Get(roomId)!;
        Assert.Equal([owner], stored.MemberGuids);
        Assert.Equal(["tab-2"], stored.MemberConnections[owner]);
        Assert.Equal(["tab-2"], ConnectionsOf(owner).Keys);
    }
}
