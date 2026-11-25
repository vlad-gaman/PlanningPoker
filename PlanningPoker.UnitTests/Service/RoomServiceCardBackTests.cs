using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomServiceCardBackTests
{
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(), new RoomChangeLog());
    private readonly RoomService _service;
    private readonly Guid _ann;
    private readonly string _roomId;

    public RoomServiceCardBackTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people);
        _ann = AddPerson();
        _roomId = _service.Create(_ann, new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 }
        }).Data!.Id;
        Assert.True(_service.Join(_ann, "c1", _roomId).IsSuccess);
    }

    private Guid AddPerson()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = PersonTypes.Developer };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    [Fact]
    public void ChangeCardBack_NewDesign_StoresItOnThePersonAndReportsAChange()
    {
        var response = _service.ChangeCardBack(_ann, "c1", CardBacks.Dots);

        Assert.True(response.IsSuccess);
        Assert.True(response.Data!.Changed);
        Assert.Equal(_roomId, response.Data.RoomId);
        Assert.Equal(_ann, response.Data.PersonGuid);
        Assert.Equal(CardBacks.Dots, response.Data.CardBack);
        Assert.Equal(CardBacks.Dots, _people.Get(_ann)!.CardBack);
    }

    [Fact]
    public void ChangeCardBack_TheSameDesign_IsASuccessThatChangesNothing()
    {
        var response = _service.ChangeCardBack(_ann, "c1", CardBacks.Default);

        Assert.True(response.IsSuccess);
        Assert.False(response.Data!.Changed);
    }

    [Fact]
    public void ChangeCardBack_ThePersonKeepsItWhenTheyJoinAgain()
    {
        // A second member keeps the room alive while Ann is away.
        Assert.True(_service.Join(AddPerson(), "c-bob", _roomId).IsSuccess);
        _service.ChangeCardBack(_ann, "c1", CardBacks.Stripes);
        _service.Exit(_ann, "c1");

        var joined = _service.Join(_ann, "c2", _roomId);

        Assert.True(joined.IsSuccess);
        Assert.Equal(CardBacks.Stripes, joined.Data!.Person.CardBack);
    }

    [Fact]
    public void ChangeCardBack_ConnectionNotInARoom_Fails()
    {
        var response = _service.ChangeCardBack(_ann, "unknown", CardBacks.Dots);

        Assert.False(response.IsSuccess);
        Assert.Equal(CardBacks.Default, _people.Get(_ann)!.CardBack);
    }

    [Fact]
    public void ChangeCardBack_UnknownPerson_FailsWithNoSession()
    {
        var response = _service.ChangeCardBack(Guid.NewGuid(), "c1", CardBacks.Dots);

        Assert.False(response.IsSuccess);
        Assert.Equal(ErrorCodes.NoSession, response.Errors.Single().Code);
    }
}
