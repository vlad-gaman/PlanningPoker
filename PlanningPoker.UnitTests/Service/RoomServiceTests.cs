using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.UnitTests.Service;

public class RoomServiceTests
{
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new PlanningPoker.Repository.Mappers.RoomEntityMapper());

    private static RoomSettings Settings(bool fun = false)
    {
        return new RoomSettings
        {
            UseFunRoomName = fun,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 7, ShowFireworks = false, MaxParticipants = 9 }
        };
    }

    private RoomService Service(IRoomNameGenerator? generator = null)
    {
        return new RoomService(
            _rooms,
            _people,
            generator ?? new StubRoomNameGenerator("unused"),
            new RoomEntityMapper(),
            new RoomMapper(),
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

    [Fact]
    public void Create_WithoutFunName_UsesGuidIdAndMakesCreatorOwnerAndFirstMember()
    {
        var owner = AddPerson();

        var response = Service().Create(owner, Settings());

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
        var response = Service(new StubRoomNameGenerator("42_big_red_cat")).Create(AddPerson(), Settings(fun: true));

        Assert.Equal("42_big_red_cat", response.Data!.Id);
    }

    [Fact]
    public void Create_IdCollision_RetriesWithNextId()
    {
        _rooms.TryAdd(new RoomEntity { Id = "taken" });
        var generator = new StubRoomNameGenerator("taken", "taken", "free");

        var response = Service(generator).Create(AddPerson(), Settings(fun: true));

        Assert.Equal("free", response.Data!.Id);
        Assert.Equal(3, generator.Calls);
    }

    [Fact]
    public void Create_EveryAttemptCollides_FailsAfterFiveAttempts()
    {
        _rooms.TryAdd(new RoomEntity { Id = "taken" });
        var generator = new StubRoomNameGenerator("taken");

        var response = Service(generator).Create(AddPerson(), Settings(fun: true));

        Assert.Equal(ErrorCodes.RoomIdGenerationFailed, Assert.Single(response.Errors).Code);
        Assert.Equal(5, generator.Calls);
    }

    [Fact]
    public void Exists_KnownRoom_ReturnsId()
    {
        var room = Service().Create(AddPerson(), Settings()).Data!;

        Assert.Equal(room.Id, Service().Exists(room.Id).Data);
    }

    [Fact]
    public void Exists_UnknownRoom_ReturnsRoomNotFound()
    {
        var response = Service().Exists("nope");

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(response.Errors).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    public void Join_InvalidIdentifier_ReturnsValidationFailed(string roomId)
    {
        var response = Service().Join(AddPerson(), "c1", roomId);

        Assert.Equal(ErrorCodes.ValidationFailed, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_UnknownRoom_ReturnsRoomNotFound()
    {
        var response = Service().Join(AddPerson(), "c1", "missing");

        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_UnknownPerson_ReturnsNoSession()
    {
        var room = Service().Create(AddPerson(), Settings()).Data!;

        var response = Service().Join(Guid.NewGuid(), "c1", room.Id);

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Join_AddsMemberMarksConnectedAndReturnsEveryone()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest", PersonTypes.Tester);
        var room = Service().Create(owner, Settings()).Data!;

        var response = Service().Join(guest, "conn-guest", room.Id);

        Assert.True(response.IsSuccess);
        Assert.Equal(["Owner", "Guest"], response.Data!.People.Select(person => person.Name));
        Assert.Equal(owner, response.Data.Room.OwnerGuid);
        Assert.Equal(guest, response.Data.Person.Guid);
        Assert.False(response.Data.VoteStatus.HasEveryoneVoted);
        Assert.Empty(response.Data.VoteStatus.Votes);
        Assert.Equal("conn-guest", _people.Get(guest)!.ConnectionId);
        Assert.True(_people.Get(guest)!.IsConnected);
    }

    [Fact]
    public void Join_AlreadyMember_DoesNotDuplicate()
    {
        var owner = AddPerson();
        var room = Service().Create(owner, Settings()).Data!;

        Service().Join(owner, "c1", room.Id);
        var response = Service().Join(owner, "c2", room.Id);

        Assert.Single(response.Data!.People);
        Assert.Equal("c2", _people.Get(owner)!.ConnectionId);
    }

    [Theory]
    [InlineData(true, PersonTypes.Developer, true)]
    [InlineData(true, PersonTypes.Observer, false)]
    [InlineData(false, PersonTypes.Developer, false)]
    public void Join_ResetsCountdownOnlyWhenVotingIsOnAndPersonIsNotObserver(bool votingEnabled, string type, bool expected)
    {
        var room = Service().Create(AddPerson(), Settings()).Data!;
        _rooms.Update(room.Id, entity => entity.VotingEnabled = votingEnabled, out _);

        var response = Service().Join(AddPerson("Guest", type), "c1", room.Id);

        Assert.Equal(expected, response.Data!.ResetCountdown);
    }

    [Fact]
    public void Exit_ConnectionIsNotTheCurrentOne_IsIgnored()
    {
        var owner = AddPerson();
        var room = Service().Create(owner, Settings()).Data!;
        Service().Join(owner, "new-connection", room.Id);

        var response = Service().Exit(owner, "old-connection");

        Assert.False(response.IsSuccess);
        Assert.Contains(owner, _rooms.Get(room.Id)!.MemberGuids);
    }

    [Fact]
    public void Exit_PersonNotInARoom_Fails()
    {
        Assert.False(Service().Exit(AddPerson(), "c1").IsSuccess);
    }

    [Fact]
    public void Exit_RegularMember_LeavesOwnerUnchanged()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var room = Service().Create(owner, Settings()).Data!;
        Service().Join(owner, "c-owner", room.Id);
        Service().Join(guest, "c-guest", room.Id);

        var response = Service().Exit(guest, "c-guest");

        Assert.True(response.IsSuccess);
        Assert.False(response.Data!.RoomDisposed);
        Assert.Null(response.Data.NewOwner);
        Assert.Equal("Guest", response.Data.Person.Name);
        Assert.Equal([owner], _rooms.Get(room.Id)!.MemberGuids);
        Assert.False(_people.Get(guest)!.IsConnected);
        Assert.Null(_people.Get(guest)!.ConnectionId);
    }

    [Fact]
    public void Exit_Owner_TransfersOwnershipToAnotherMember()
    {
        var owner = AddPerson("Owner");
        var guest = AddPerson("Guest");
        var room = Service().Create(owner, Settings()).Data!;
        Service().Join(owner, "c-owner", room.Id);
        Service().Join(guest, "c-guest", room.Id);

        var response = Service().Exit(owner, "c-owner");

        Assert.False(response.Data!.RoomDisposed);
        Assert.Equal(guest, response.Data.NewOwner!.Guid);
        Assert.Equal(guest, _rooms.Get(room.Id)!.OwnerGuid);
    }

    [Fact]
    public void Exit_LastMember_DisposesRoom()
    {
        var owner = AddPerson();
        var room = Service().Create(owner, Settings()).Data!;
        Service().Join(owner, "c1", room.Id);

        var response = Service().Exit(owner, "c1");

        Assert.True(response.Data!.RoomDisposed);
        Assert.Null(response.Data.NewOwner);
        Assert.Equal(room.Id, response.Data.RoomId);
        Assert.Null(_rooms.Get(room.Id));
    }
}
