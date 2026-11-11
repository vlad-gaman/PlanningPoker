using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>
/// The offgrid life of a person: a dropped connection keeps them online for the grace period, then offgrid with their vote,
/// then removed once the removal window passes. Everything is driven through the service's public entry points and a fake clock.
/// </summary>
public class RoomServicePresenceTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private static readonly string[] ThreeDevs = [PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer];
    private static readonly string[] TwoDevs = [PersonTypes.Developer, PersonTypes.Developer];

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;

    public RoomServicePresenceTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people, time: _time);
    }

    private Guid AddPerson(string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    /// <summary>A room with one connected member per type, on connections "c1", "c2" and so on.</summary>
    private (string RoomId, Guid[] People) RoomWith(params string[] types)
    {
        var people = types.Select(AddPerson).ToArray();
        var settings = new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 }
        };
        var roomId = _service.Create(people[0], settings).Data!.Id;
        for (var i = 0; i < people.Length; i++)
        {
            Assert.True(_service.Join(people[i], $"c{i + 1}", roomId).IsSuccess);
        }

        return (roomId, people);
    }

    private PresenceSweepResult SweepAfter(TimeSpan elapsed)
    {
        _time.Advance(elapsed);
        return _service.SweepPresence();
    }

    // --- dropping ---

    [Fact]
    public void Disconnect_LastConnectionOfAVoter_KeepsTheMemberAndTheirVote()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[2], "c3", "5");

        var disconnected = _service.Disconnect(people[2], "c3");

        Assert.True(disconnected.IsSuccess);
        var room = _rooms.Get(roomId)!;
        Assert.Contains(people[2], room.MemberGuids);
        Assert.Equal("5", room.Votes[people[2]]);
        Assert.Equal(_time.GetUtcNow(), room.DroppedAt[people[2]]);
        Assert.Empty(_people.Get(people[2])!.Connections);
    }

    [Fact]
    public void Disconnect_OneOfTwoTabs_DoesNotDropThePerson()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Join(people[1], "c2-second", roomId);

        _service.Disconnect(people[1], "c2");
        SweepAfter(Window * 2);

        var room = _rooms.Get(roomId)!;
        Assert.Empty(room.DroppedAt);
        Assert.Contains(people[1], room.MemberGuids);
    }

    [Fact]
    public void Disconnect_UnknownConnection_ReturnsNotInRoom()
    {
        var (_, people) = RoomWith(TwoDevs);

        var disconnected = _service.Disconnect(people[0], "nope");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(disconnected.Errors).Code);
    }

    [Fact]
    public void Disconnect_UnknownPerson_ReturnsNoSession()
    {
        var disconnected = _service.Disconnect(Guid.NewGuid(), "c1");

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(disconnected.Errors).Code);
    }

    [Fact]
    public void Disconnect_StampsLastSeenAtWithNow()
    {
        var (_, people) = RoomWith(TwoDevs);
        _time.Advance(TimeSpan.FromMinutes(3));

        _service.Disconnect(people[0], "c1");

        Assert.Equal(_time.GetUtcNow(), _people.Get(people[0])!.LastSeenAt);
    }

    // --- grace period ---

    [Fact]
    public void SweepPresence_WithinTheGracePeriod_AnnouncesNothing()
    {
        var (_, people) = RoomWith(TwoDevs);
        _service.Disconnect(people[1], "c2");

        var sweep = SweepAfter(Grace - TimeSpan.FromMilliseconds(1));

        Assert.Empty(sweep.Offgrid);
        Assert.Empty(sweep.Removed);
    }

    [Fact]
    public void Join_RefreshWithinTheGracePeriod_RestoresThePersonWithTheirVoteAndAnnouncesNothing()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        _time.Advance(TimeSpan.FromSeconds(3));

        var joined = _service.Join(people[1], "c2-new", roomId).Data!;

        Assert.False(joined.PersonAdded);
        Assert.False(joined.WasOffgrid);
        Assert.Equal("8", joined.MyMark);
        Assert.Contains(joined.People, person => person.Guid == people[1] && !person.IsOffgrid);
        Assert.Empty(_rooms.Get(roomId)!.DroppedAt);
        Assert.Empty(SweepAfter(Window * 2).Offgrid);
    }

    [Fact]
    public void Vote_WhileAnotherPersonIsInTheirGracePeriod_StillWaitsForThem()
    {
        var (_, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Disconnect(people[2], "c3");
        _time.Advance(TimeSpan.FromSeconds(5));

        var voted = _service.Vote(people[1], "c2", "8").Data!;

        Assert.False(voted.VoteStatus.HasEveryoneVoted);
        Assert.Null(voted.FollowUp.Countdown);
    }

    // --- offgrid ---

    [Fact]
    public void SweepPresence_AfterTheGracePeriod_ReportsThePersonOffgridOnceAndKeepsTheirVote()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");

        var first = SweepAfter(Grace);
        var second = SweepAfter(TimeSpan.FromSeconds(1));

        var offgrid = Assert.Single(first.Offgrid);
        Assert.Equal(roomId, offgrid.RoomId);
        Assert.Equal(people[1], offgrid.Person.Guid);
        Assert.True(offgrid.Person.IsOffgrid);
        Assert.Empty(second.Offgrid);
        Assert.Equal("8", _rooms.Get(roomId)!.Votes[people[1]]);
    }

    [Fact]
    public void Join_OfSomeoneNew_ListsTheOffgridPersonAsOffgridWithTheirHiddenVote()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        SweepAfter(Grace);

        var joined = _service.Join(AddPerson(), "c9", roomId).Data!;

        Assert.Contains(joined.People, person => person.Guid == people[1] && person.IsOffgrid);
        Assert.Contains(joined.People, person => person.Guid == people[0] && !person.IsOffgrid);
        Assert.Equal(RoomConstraints.HiddenVote, joined.VoteStatus.Votes[people[1]]);
    }

    [Fact]
    public void Join_ReturningFromOffgrid_RestoresThePersonAndReportsTheyWereOffgrid()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        SweepAfter(Grace + TimeSpan.FromMinutes(1));

        var joined = _service.Join(people[1], "c2-new", roomId).Data!;

        Assert.False(joined.PersonAdded);
        Assert.True(joined.WasOffgrid);
        Assert.Equal("8", joined.MyMark);
        Assert.Contains(joined.People, person => person.Guid == people[1] && !person.IsOffgrid);
        var room = _rooms.Get(roomId)!;
        Assert.Empty(room.DroppedAt);
        Assert.Empty(room.OffgridGuids);
    }

    [Fact]
    public void Join_ReturningAfterTheRevealFromOffgrid_SeesTheResultsIncludingTheirOwnVote()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[2], "c3", "8");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);
        _service.Vote(people[0], "c1", "5");
        _service.ForceShowVotes(people[0], "c1");

        var joined = _service.Join(people[2], "c3-new", roomId).Data!;

        Assert.True(joined.VoteStatus.VotingFinished);
        Assert.Equal("8", joined.VoteStatus.Votes[people[2]]);
        Assert.Equal("8", joined.MyMark);
        Assert.NotNull(joined.VoteStatus.Statistics);
    }

    [Fact]
    public void Join_ReturningWhileVotingIsOpen_SeesTheCurrentVoteStatus()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);

        var joined = _service.Join(people[2], "c3-new", roomId).Data!;

        Assert.False(joined.VoteStatus.VotingFinished);
        Assert.Equal(RoomConstraints.HiddenVote, joined.VoteStatus.Votes[people[0]]);
        Assert.False(joined.VoteStatus.HasEveryoneVoted);
    }

    // --- voting with offgrid people ---

    [Fact]
    public void SweepPresence_WhenTheOnlyPersonYetToVoteGoesOffgrid_StartsTheCountdown()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[2], "c3");

        var sweep = SweepAfter(Grace);

        var countdown = Assert.Single(sweep.Offgrid).FollowUp.Countdown;
        Assert.NotNull(countdown);
        Assert.Equal(5, countdown.Seconds);
        Assert.Equal(TickOutcome.Ticked, _service.Tick(roomId, countdown.Revision).Outcome);
    }

    [Fact]
    public void Vote_WhenTheOthersHaveVotedAndTheRestAreOffgrid_StartsTheCountdown()
    {
        var (_, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);

        var voted = _service.Vote(people[1], "c2", "8").Data!;

        Assert.True(voted.VoteStatus.HasEveryoneVoted);
        Assert.NotNull(voted.FollowUp.Countdown);
    }

    [Fact]
    public void SweepPresence_WhenAVoterWhoBlocksNobodyGoesOffgrid_StartsNoSecondCountdown()
    {
        var (_, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        var started = _service.Vote(people[2], "c3", "3").Data!.FollowUp.Countdown!;
        _service.Disconnect(people[2], "c3");

        var sweep = SweepAfter(Grace);

        Assert.Null(Assert.Single(sweep.Offgrid).FollowUp.Countdown);
        Assert.Equal(TickOutcome.Ticked, _service.Tick(_rooms.Ids().Single(), started.Revision).Outcome);
    }

    [Fact]
    public void SweepPresence_WhenEveryoneIsOffgrid_DoesNotStartACountdownAndKeepsTheRoundOpen()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Disconnect(people[0], "c1");
        _service.Disconnect(people[1], "c2");

        var sweep = SweepAfter(Grace);

        Assert.Equal(2, sweep.Offgrid.Count);
        Assert.All(sweep.Offgrid, offgrid => Assert.Null(offgrid.FollowUp.Countdown));
        Assert.Null(sweep.Offgrid[0].FollowUp.Revealed);
        Assert.True(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void ForceShowVotes_IncludesTheVoteOfAnOffgridVoterAndLeavesOutAnOffgridNonVoter()
    {
        var (_, people) = RoomWith(ThreeDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);
        _service.Vote(people[0], "c1", "5");

        var status = _service.ForceShowVotes(people[0], "c1").Data!.VoteStatus!;

        Assert.Equal(2, status.Votes.Count);
        Assert.Equal("8", status.Votes[people[1]]);
        Assert.DoesNotContain(people[2], status.Votes.Keys);
        Assert.True(status.HasEveryoneVoted);
        Assert.Equal(["5", "8"], status.Statistics!.All.Marks.Select(share => share.Mark));
    }

    [Fact]
    public void ClearVotes_AlsoClearsTheVoteOfAnOffgridPerson()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        SweepAfter(Grace);

        _service.ClearVotes(people[0], "c1");
        var joined = _service.Join(people[1], "c2-new", roomId).Data!;

        Assert.Null(joined.MyMark);
        Assert.Empty(joined.VoteStatus.Votes);
        Assert.Contains(joined.People, person => person.Guid == people[1]);
    }

    [Fact]
    public void Join_OffgridNonVoterReturningWhileVotingIsOpen_CancelsTheRunningCountdown()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[2], "c3");
        var started = Assert.Single(SweepAfter(Grace).Offgrid).FollowUp.Countdown!;

        var joined = _service.Join(people[2], "c3-new", roomId).Data!;

        Assert.True(joined.ResetCountdown);
        Assert.False(joined.VoteStatus.HasEveryoneVoted);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, started.Revision).Outcome);
    }

    [Fact]
    public void Join_OffgridVoterReturningWhileTheOthersCountdownRuns_KeepsTheCountdown()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[2], "c3", "3");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);
        _service.Vote(people[0], "c1", "5");
        var started = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        var joined = _service.Join(people[2], "c3-new", roomId).Data!;

        Assert.False(joined.ResetCountdown);
        Assert.Equal(TickOutcome.Ticked, _service.Tick(roomId, started.Revision).Outcome);
    }

    [Fact]
    public void Join_PersonStillInTheirGracePeriod_DoesNotTouchARunningCountdown()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var started = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;
        _service.Disconnect(people[1], "c2");
        _time.Advance(TimeSpan.FromSeconds(2));

        _service.Join(people[1], "c2-new", roomId);

        Assert.Equal(TickOutcome.Ticked, _service.Tick(roomId, started.Revision).Outcome);
    }

    // --- removal ---

    [Fact]
    public void SweepPresence_AfterTheRemovalWindow_RemovesThePersonAndTheirVote()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[2], "c3", "8");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);

        var sweep = SweepAfter(Window - Grace);

        var removed = Assert.Single(sweep.Removed);
        Assert.Equal(roomId, removed.RoomId);
        Assert.Equal(people[2], removed.Person.Guid);
        Assert.True(removed.PersonLeft);
        var room = _rooms.Get(roomId)!;
        Assert.DoesNotContain(people[2], room.MemberGuids);
        Assert.DoesNotContain(people[2], room.Votes.Keys);
        Assert.Empty(room.DroppedAt);
        Assert.Empty(room.OffgridGuids);
    }

    [Fact]
    public void SweepPresence_JustBeforeTheRemovalWindowEnds_KeepsThePerson()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Disconnect(people[2], "c3");

        var sweep = SweepAfter(Window - TimeSpan.FromSeconds(1));

        Assert.Empty(sweep.Removed);
        Assert.Contains(people[2], _rooms.Get(roomId)!.MemberGuids);
    }

    [Fact]
    public void SweepPresence_WhenAPersonIsDueForBothOffgridAndRemoval_OnlyRemovesThem()
    {
        var (_, people) = RoomWith(ThreeDevs);
        _service.Disconnect(people[2], "c3");

        var sweep = SweepAfter(Window);

        Assert.Empty(sweep.Offgrid);
        Assert.Single(sweep.Removed);
    }

    [Fact]
    public void SweepPresence_RemovingAnOffgridVoter_RestartsTheCountdownOfTheRest()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Vote(people[2], "c3", "3");
        _service.Disconnect(people[2], "c3");
        SweepAfter(Grace);
        _service.Vote(people[0], "c1", "5");
        var started = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        var removed = Assert.Single(SweepAfter(Window).Removed);

        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, started.Revision).Outcome);
        var restarted = removed.FollowUp.Countdown;
        Assert.NotNull(restarted);
        Assert.NotEqual(started.Revision, restarted.Revision);
    }

    [Fact]
    public void SweepPresence_RemovingTheOwner_HandsTheRoomToAConnectedMember()
    {
        var (roomId, people) = RoomWith(ThreeDevs);
        _service.Disconnect(people[0], "c1");
        _service.Disconnect(people[1], "c2");

        var sweep = SweepAfter(Window);

        var removedOwner = Assert.Single(sweep.Removed, result => result.Person.Guid == people[0]);
        Assert.Equal(people[2], removedOwner.NewOwner!.Guid);
        Assert.Equal(people[2], _rooms.Get(roomId)!.OwnerGuid);
        Assert.Null(Assert.Single(sweep.Removed, result => result.Person.Guid == people[1]).NewOwner);
    }

    [Fact]
    public void SweepPresence_OffgridOwner_StaysOwnerUntilRemoved()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Disconnect(people[0], "c1");

        var sweep = SweepAfter(Grace);

        Assert.Single(sweep.Offgrid);
        Assert.Equal(people[0], _rooms.Get(roomId)!.OwnerGuid);
    }

    [Fact]
    public void Join_AfterBeingRemoved_AddsThePersonAsNewWithoutTheirOldVote()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        SweepAfter(Window);

        var joined = _service.Join(people[1], "c2-new", roomId).Data!;

        Assert.True(joined.PersonAdded);
        Assert.False(joined.WasOffgrid);
        Assert.Null(joined.MyMark);
        Assert.Empty(joined.VoteStatus.Votes);
    }

    [Fact]
    public void Join_OneSecondBeforeTheRemovalWindowEnds_StillRestoresThePerson()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Vote(people[1], "c2", "8");
        _service.Disconnect(people[1], "c2");
        SweepAfter(Window - TimeSpan.FromSeconds(1));

        var joined = _service.Join(people[1], "c2-new", roomId).Data!;

        Assert.False(joined.PersonAdded);
        Assert.True(joined.WasOffgrid);
        Assert.Equal("8", joined.MyMark);
    }

    // --- rooms ---

    [Fact]
    public void Join_SoloPersonRefreshes_KeepsTheRoom()
    {
        var (roomId, people) = RoomWith([PersonTypes.Developer]);
        _service.Vote(people[0], "c1", "5");

        _service.Disconnect(people[0], "c1");
        _time.Advance(TimeSpan.FromSeconds(2));
        var sweep = _service.SweepPresence();
        var joined = _service.Join(people[0], "c1-new", roomId);

        Assert.Empty(sweep.DisposedRoomIds);
        Assert.True(joined.IsSuccess);
        Assert.Equal("5", joined.Data!.MyMark);
    }

    [Fact]
    public void SweepPresence_RoomWithNobodyOnlineForTheEmptyRoomLifetime_DeletesTheRoom()
    {
        var (roomId, people) = RoomWith([PersonTypes.Developer]);
        _service.Disconnect(people[0], "c1");

        var offgrid = SweepAfter(Grace);
        var stillThere = SweepAfter(TimeSpan.FromSeconds(9));
        var deleted = SweepAfter(TimeSpan.FromSeconds(1));

        Assert.Single(offgrid.Offgrid);
        Assert.Empty(stillThere.DisposedRoomIds);
        Assert.Equal(roomId, Assert.Single(deleted.DisposedRoomIds));
        Assert.Null(_rooms.Get(roomId));
        Assert.Equal(ErrorCodes.RoomNotFound, Assert.Single(_service.Join(people[0], "c1-new", roomId).Errors).Code);
    }

    [Fact]
    public void SweepPresence_EveryoneInTheirGracePeriod_DoesNotDeleteTheRoom()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Disconnect(people[0], "c1");
        _service.Disconnect(people[1], "c2");

        var sweep = SweepAfter(Grace - TimeSpan.FromMilliseconds(1));

        Assert.Empty(sweep.DisposedRoomIds);
        Assert.NotNull(_rooms.Get(roomId));
    }

    [Fact]
    public void SweepPresence_AnObserverOnline_KeepsTheRoomAlive()
    {
        var (roomId, people) = RoomWith(PersonTypes.Developer, PersonTypes.Observer);
        _service.Disconnect(people[0], "c1");

        var sweep = SweepAfter(Window);

        Assert.Empty(sweep.DisposedRoomIds);
        Assert.NotNull(_rooms.Get(roomId));
        Assert.Single(sweep.Removed);
    }

    [Fact]
    public void SweepPresence_RoomNobodyHasJoinedYet_IsLeftToTheAbandonedSweep()
    {
        var owner = AddPerson();
        var settings = new RoomSettings { Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 } };
        var roomId = _service.Create(owner, settings).Data!.Id;

        var sweep = SweepAfter(TimeSpan.FromHours(1));

        Assert.Empty(sweep.DisposedRoomIds);
        Assert.NotNull(_rooms.Get(roomId));
    }

    // --- takeover ---

    [Fact]
    public void Disconnect_OldConnectionAfterANewTabTookOver_DoesNotMakeThePersonOffgrid()
    {
        var (roomId, people) = RoomWith(TwoDevs);
        _service.Join(people[1], "c2-new", roomId);

        _service.Disconnect(people[1], "c2");
        var sweep = SweepAfter(Window);

        Assert.Empty(sweep.Offgrid);
        Assert.Empty(sweep.Removed);
        Assert.Empty(_rooms.Get(roomId)!.DroppedAt);
    }

    [Fact]
    public void Disconnect_OldConnectionClosingBeforeTheNewTabJoins_IsInvisibleOnceTheNewTabJoinsInTime()
    {
        var (roomId, people) = RoomWith(TwoDevs);

        _service.Disconnect(people[1], "c2");
        _time.Advance(TimeSpan.FromSeconds(1));
        _service.Join(people[1], "c2-new", roomId);
        var sweep = SweepAfter(Window);

        Assert.Empty(sweep.Offgrid);
        Assert.Empty(sweep.Removed);
    }

    // --- settings ---

    [Fact]
    public void PresenceOptions_DefaultToTenSecondsTenMinutesAndTenSeconds()
    {
        var options = new PresenceOptions();

        Assert.Equal(TimeSpan.FromSeconds(10), options.GracePeriod);
        Assert.Equal(TimeSpan.FromMinutes(10), options.RemovalWindow);
        Assert.Equal(TimeSpan.FromSeconds(10), options.EmptyRoomLifetime);
    }

    [Fact]
    public void SweepPresence_UsesTheConfiguredRemovalWindow()
    {
        var service = RoomServiceFactory.Create(_rooms, _people, time: _time, presence: new PresenceOptions { RemovalWindow = TimeSpan.FromMinutes(1) });
        var owner = AddPerson();
        var guest = AddPerson();
        var settings = new RoomSettings { Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 } };
        var roomId = service.Create(owner, settings).Data!.Id;
        service.Join(owner, "o1", roomId);
        service.Join(guest, "g1", roomId);
        service.Disconnect(guest, "g1");
        _time.Advance(TimeSpan.FromMinutes(1));

        var sweep = service.SweepPresence();

        Assert.Equal(guest, Assert.Single(sweep.Removed).Person.Guid);
    }
}
