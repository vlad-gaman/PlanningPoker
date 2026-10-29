using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class RoomEventPublisherTests
{
    private const string RoomGroup = "group:room";
    private const string JoinerConnection = "joiner-conn";

    private static readonly PersonResponseMapper PersonMapper = new();
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };
    private static readonly Person Bob = new() { Guid = Guid.NewGuid(), Name = "Bob", PersonType = "test" };

    private readonly FakeHubContext _hub = new();
    private readonly RoomEventPublisher _publisher;

    public RoomEventPublisherTests()
    {
        _publisher = new RoomEventPublisher(
            _hub,
            new PersonJoinedEventMapper(PersonMapper),
            new CountdownResetEventMapper(),
            new PersonExitedEventMapper(PersonMapper),
            new OwnershipTransferredEventMapper(PersonMapper),
            new RoomDisposedEventMapper());
    }

    [Fact]
    public async Task PublishPersonJoined_CountdownReset_GoesToRoomExceptTheJoiner()
    {
        await _publisher.PublishPersonJoinedAsync(RoomResults.Join(Bob, [Ann, Bob], resetCountdown: true, countdownSeconds: 7), JoinerConnection);

        var reset = _hub.Clients.Sent.Single(message => message.Method == HubEvents.CountdownReset);
        Assert.Equal(RoomGroup, reset.Target);
        Assert.Equal([JoinerConnection], reset.Excluded);
        Assert.Equal(7, Assert.IsType<CountdownResetEvent>(reset.Payload).Countdown);
    }

    [Fact]
    public async Task PublishPersonJoined_AlwaysAnnouncesThePersonToOthers()
    {
        await _publisher.PublishPersonJoinedAsync(RoomResults.Join(Bob, [Ann, Bob], resetCountdown: false), JoinerConnection);

        var joined = Assert.Single(_hub.Clients.Sent);
        Assert.Equal(HubEvents.PersonJoined, joined.Method);
        Assert.Equal(RoomGroup, joined.Target);
        Assert.Equal([JoinerConnection], joined.Excluded);
        Assert.Equal("Bob", Assert.IsType<PersonJoinedEvent>(joined.Payload).Person.Name);
    }

    [Fact]
    public async Task PublishPersonJoined_PersonAlreadyInRoom_SendsNothing()
    {
        // A second tab or a reconnect: the person was already a member, so neither PersonJoined nor CountdownReset is sent.
        await _publisher.PublishPersonJoinedAsync(RoomResults.Join(Bob, [Ann, Bob], resetCountdown: true, personAdded: false), JoinerConnection);

        Assert.Empty(_hub.Clients.Sent);
    }

    [Fact]
    public async Task PublishPersonExited_LastMember_SendsRoomDisposedToRoom()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Ann, roomDisposed: true));

        var disposed = Assert.Single(_hub.Clients.Sent, message => message.Method == HubEvents.RoomDisposed);
        Assert.Equal(RoomGroup, disposed.Target);
        Assert.Empty(disposed.Excluded);
        Assert.Equal("room", Assert.IsType<RoomDisposedEvent>(disposed.Payload).RoomId);
    }

    [Fact]
    public async Task PublishPersonExited_RoomStillHasMembers_SendsNoRoomDisposed()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Ann, roomDisposed: false));

        Assert.DoesNotContain(_hub.Clients.Sent, message => message.Method == HubEvents.RoomDisposed);
        Assert.Equal(HubEvents.PersonExited, Assert.Single(_hub.Clients.Sent).Method);
    }

    [Fact]
    public async Task PublishPersonExited_WithNewOwner_SendsOwnershipTransferredToRoom()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Ann, newOwner: Bob));

        var transferred = Assert.Single(_hub.Clients.Sent, message => message.Method == HubEvents.OwnershipTransferred);
        Assert.Equal(RoomGroup, transferred.Target);
        Assert.Equal("Bob", Assert.IsType<OwnershipTransferredEvent>(transferred.Payload).NewOwner.Name);
    }

    [Fact]
    public async Task PublishPersonExited_WithoutNewOwner_SendsNoOwnershipTransferred()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Ann, newOwner: null));

        Assert.DoesNotContain(_hub.Clients.Sent, message => message.Method == HubEvents.OwnershipTransferred);
    }

    [Fact]
    public async Task PublishPersonExited_PersonStillConnectedElsewhereInRoom_SendsNothing()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Ann, personLeft: false));

        Assert.Empty(_hub.Clients.Sent);
    }
}
