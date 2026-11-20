using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>
/// Which operations count as room activity (the Abandoned rule): any change to the state of the room does, including connection and
/// presence changes; refused or no-op calls do not. Each test runs one operation and checks whether the room was reported and stamped.
/// </summary>
public class RoomActivityTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMinutes(1);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly RoomChangeLog _log = new();
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms;
    private readonly RoomService _service;
    private readonly PresenceService _presence;
    private readonly string _roomId;
    private readonly Guid _owner;
    private readonly Guid _guest;
    private readonly Guid _observer;

    public RoomActivityTests()
    {
        _rooms = new RoomRepository(new RoomEntityCopyMapper(), _log);
        _service = RoomServiceFactory.Create(_rooms, _people, time: _time);
        _presence = PresenceServiceFactory.Create(_rooms, _people, _service, _time);
        _owner = AddPerson(PersonTypes.Developer);
        _guest = AddPerson(PersonTypes.Developer);
        _observer = AddPerson(PersonTypes.Observer);
        var settings = new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 0, ShowFireworks = false, MaxParticipants = 9 }
        };
        _roomId = _service.Create(_owner, settings).Data!.Id;
        Assert.True(_service.Join(_owner, "c-owner", _roomId).IsSuccess);
        Assert.True(_service.Join(_observer, "c-observer", _roomId).IsSuccess);
    }

    private Guid AddPerson(string type)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    /// <summary>Runs the operation a minute later and says whether the room was reported to the change log and stamped as active.</summary>
    private (bool Reported, bool Stamped) Run(Action operation)
    {
        while (_log.Changes.TryRead(out _))
        {
        }

        var before = _rooms.Get(_roomId)!.LastActivityAt;
        _time.Advance(Step);
        operation();

        var reported = false;
        while (_log.Changes.TryRead(out var id))
        {
            reported |= id == _roomId;
        }

        var stamped = _rooms.Get(_roomId) is { } room && room.LastActivityAt != before;
        return (reported, stamped);
    }

    private static void AssertActive((bool Reported, bool Stamped) result)
    {
        Assert.True(result.Reported);
        Assert.True(result.Stamped);
    }

    private static void AssertIdle((bool Reported, bool Stamped) result)
    {
        Assert.False(result.Reported);
        Assert.False(result.Stamped);
    }

    [Fact]
    public void Join_AnAdmittedPerson_CountsAsActivity()
    {
        AssertActive(Run(() => _service.Join(_guest, "c-guest", _roomId)));
    }

    [Fact]
    public void Join_AnotherConnectionOfAMember_CountsAsActivity()
    {
        AssertActive(Run(() => _service.Join(_owner, "c-second", _roomId)));
    }

    [Fact]
    public void Join_ADisposedRoom_DoesNotCount()
    {
        _rooms.Edit(_roomId, room => room.IsDisposed = true);

        AssertIdle(Run(() => _service.Join(_guest, "c-guest", _roomId)));
    }

    [Fact]
    public void Exit_AMemberLeaving_CountsAsActivity()
    {
        AssertActive(Run(() => _service.Exit(_observer, "c-observer")));
    }

    [Fact]
    public void Exit_ALeftConnection_DoesNotCount()
    {
        _service.Exit(_observer, "c-observer");

        AssertIdle(Run(() => _service.Exit(_observer, "c-observer")));
    }

    [Fact]
    public void Vote_ACastVote_CountsAsActivity()
    {
        AssertActive(Run(() => _service.Vote(_owner, "c-owner", "5")));
    }

    [Fact]
    public void Vote_AnObserver_DoesNotCount()
    {
        AssertIdle(Run(() => _service.Vote(_observer, "c-observer", "5")));
    }

    [Fact]
    public void Vote_AMarkNotInTheCardSet_DoesNotCount()
    {
        AssertIdle(Run(() => _service.Vote(_owner, "c-owner", "not-a-card")));
    }

    [Fact]
    public void ClearVotes_CountsAsActivity()
    {
        AssertActive(Run(() => _service.ClearVotes(_owner, "c-owner")));
    }

    [Fact]
    public void ForceShowVotes_OpenVoting_CountsAsActivity()
    {
        AssertActive(Run(() => _service.ForceShowVotes(_owner, "c-owner")));
    }

    [Fact]
    public void ForceShowVotes_AlreadyRevealed_DoesNotCount()
    {
        _service.ForceShowVotes(_owner, "c-owner");

        AssertIdle(Run(() => _service.ForceShowVotes(_owner, "c-owner")));
    }

    [Fact]
    public void ChangePersonType_ANewRole_CountsAsActivity()
    {
        AssertActive(Run(() => _service.ChangePersonType(_owner, "c-owner", PersonTypes.Observer)));
    }

    [Fact]
    public void ChangePersonType_TheSameRole_DoesNotCount()
    {
        AssertIdle(Run(() => _service.ChangePersonType(_owner, "c-owner", PersonTypes.Developer)));
    }

    [Fact]
    public void UpdateConfiguration_ByTheOwner_CountsAsActivity()
    {
        var configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 3, ShowFireworks = true, MaxParticipants = 9 };

        AssertActive(Run(() => _service.UpdateConfiguration(_owner, "c-owner", configuration)));
    }

    [Fact]
    public void UpdateConfiguration_ByANonOwner_DoesNotCount()
    {
        var configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 3, ShowFireworks = true, MaxParticipants = 9 };

        AssertIdle(Run(() => _service.UpdateConfiguration(_observer, "c-observer", configuration)));
    }

    [Fact]
    public void TransferOwnership_ToAnotherMember_CountsAsActivity()
    {
        AssertActive(Run(() => _service.TransferOwnership(_owner, "c-owner", _observer)));
    }

    [Fact]
    public void TransferOwnership_ToTheOwner_DoesNotCount()
    {
        AssertIdle(Run(() => _service.TransferOwnership(_owner, "c-owner", _owner)));
    }

    [Fact]
    public void Tick_AStaleRevision_DoesNotCount()
    {
        AssertIdle(Run(() => _service.Tick(_roomId, revision: -1)));
    }

    [Fact]
    public void Tick_ARunningCountdown_CountsAsActivity()
    {
        var configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 };
        _service.UpdateConfiguration(_owner, "c-owner", configuration);
        var follow = _service.Vote(_owner, "c-owner", "5").Data!.FollowUp;
        Assert.NotNull(follow.Countdown);

        AssertActive(Run(() => _service.Tick(_roomId, follow.Countdown.Revision)));
    }

    [Fact]
    public void FollowUpAfterChange_NothingToDecide_DoesNotCount()
    {
        AssertIdle(Run(() => _service.FollowUpAfterChange(_roomId)));
    }

    [Fact]
    public void FollowUpAfterChange_EveryoneVoted_CountsAsActivity()
    {
        _rooms.Edit(_roomId, room => room.Votes[_owner] = "5");

        AssertActive(Run(() => _service.FollowUpAfterChange(_roomId)));
    }

    [Fact]
    public void Disconnect_ALastConnection_CountsAsActivity()
    {
        AssertActive(Run(() => _presence.Disconnect(_observer, "c-observer")));
    }

    [Fact]
    public void Disconnect_AnUnknownConnection_DoesNotCount()
    {
        AssertIdle(Run(() => _presence.Disconnect(_observer, "c-unknown")));
    }

    [Fact]
    public void SweepPresence_NobodyDue_DoesNotCount()
    {
        AssertIdle(Run(() => _presence.SweepPresence()));
    }

    [Fact]
    public void SweepPresence_AMemberGoingOffgrid_CountsAsActivity()
    {
        _presence.Disconnect(_observer, "c-observer");

        var grace = new PresenceOptions().GracePeriod;
        AssertActive(Run(() =>
        {
            _time.Advance(grace);
            _presence.SweepPresence();
        }));
    }
}
