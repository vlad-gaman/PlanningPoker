using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomServiceManagementTests
{
    private static readonly string[] TwoDevs = [PersonTypes.Developer, PersonTypes.Developer];
    private static readonly string[] ThreeDevs = [PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer];

    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;

    public RoomServiceManagementTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people);
    }

    private static RoomConfiguration Configuration(string cardSet = "linear", int countdown = 5, bool fireworks = false, int max = 9)
    {
        return new RoomConfiguration { CardSet = cardSet, CountdownSeconds = countdown, ShowFireworks = fireworks, MaxParticipants = max };
    }

    private Guid AddPerson(string type = PersonTypes.Developer, string name = "p")
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = name, PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    /// <summary>A room with one connected member per type; the first is the owner. Connections are "c1", "c2" and so on.</summary>
    private (string RoomId, Guid[] People) RoomWith(int countdown, params string[] types)
    {
        var people = types.Select((type, index) => AddPerson(type, $"Person {index + 1}")).ToArray();
        var settings = new RoomSettings { UseFunRoomName = false, Configuration = Configuration(countdown: countdown) };
        var roomId = _service.Create(people[0], settings).Data!.Id;
        for (var i = 0; i < people.Length; i++)
        {
            Assert.True(_service.Join(people[i], $"c{i + 1}", roomId).IsSuccess);
        }

        return (roomId, people);
    }

    private string TypeOf(Guid personGuid) => _people.Get(personGuid)!.PersonType;

    // ChangePersonType

    [Fact]
    public void ChangePersonType_SetsTheTypeAndReportsIt()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.ChangePersonType(people[1], "c2", PersonTypes.Tester);

        Assert.True(response.IsSuccess);
        Assert.True(response.Data!.Changed);
        Assert.Equal(roomId, response.Data.RoomId);
        Assert.Equal(PersonTypes.Tester, response.Data.Person.PersonType);
        Assert.Equal(people[1], response.Data.Person.Guid);
        Assert.Equal(PersonTypes.Tester, TypeOf(people[1]));
    }

    [Fact]
    public void ChangePersonType_SameType_IsANoOpThatLeavesTheRoomAlone()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var before = _rooms.Get(roomId)!.Revision;

        var response = _service.ChangePersonType(people[1], "c2", PersonTypes.Developer);

        Assert.True(response.IsSuccess);
        Assert.False(response.Data!.Changed);
        Assert.Equal(before, _rooms.Get(roomId)!.Revision);
        Assert.Same(VotingFollowUp.None, response.Data.FollowUp);
    }

    [Fact]
    public void ChangePersonType_UnknownConnection_FailsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var response = _service.ChangePersonType(people[0], "elsewhere", PersonTypes.Tester);

        Assert.Equal(ErrorCodes.NotInRoom, response.Errors.Single().Code);
        Assert.Equal(PersonTypes.Developer, TypeOf(people[0]));
    }

    [Fact]
    public void ChangePersonType_UnknownPerson_FailsNoSession()
    {
        var response = _service.ChangePersonType(Guid.NewGuid(), "c1", PersonTypes.Tester);

        Assert.Equal(ErrorCodes.NoSession, response.Errors.Single().Code);
    }

    [Fact]
    public void ChangePersonType_WhileVotingIsOpen_CancelsARunningCountdown()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");
        var countdown = _service.Vote(people[2], "c3", "5").Data!.FollowUp.Countdown!;

        var response = _service.ChangePersonType(people[0], "c1", PersonTypes.Tester);

        Assert.True(_rooms.Get(roomId)!.Revision > countdown.Revision);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, countdown.Revision).Outcome);
        Assert.Null(response.Data!.VoteStatus.Countdown);
    }

    [Fact]
    public void ChangePersonType_ThatLeavesEveryoneVoted_RestartsTheCountdown()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.ChangePersonType(people[1], "c2", PersonTypes.Observer);

        var start = response.Data!.FollowUp.Countdown!;
        Assert.Equal(5, start.Seconds);
        Assert.Equal(_rooms.Get(roomId)!.Revision, start.Revision);
        Assert.True(response.Data.VoteStatus.HasEveryoneVoted);
        Assert.Null(response.Data.VoteStatus.Countdown);
    }

    [Fact]
    public void ChangePersonType_ThatLeavesEveryoneVotedWithCountdownZero_RevealsAtOnce()
    {
        var (roomId, people) = RoomWith(0, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.ChangePersonType(people[1], "c2", PersonTypes.Observer);

        Assert.NotNull(response.Data!.FollowUp.Revealed);
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
        Assert.Equal("5", response.Data.FollowUp.Revealed!.Votes[people[0]]);
    }

    [Fact]
    public void ChangePersonType_ThatLeavesSomeoneToVote_StartsNoCountdown()
    {
        var (_, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");

        var response = _service.ChangePersonType(people[2], "c3", PersonTypes.Tester);

        Assert.Same(VotingFollowUp.None, response.Data!.FollowUp);
        Assert.False(response.Data.VoteStatus.HasEveryoneVoted);
    }

    [Fact]
    public void ChangePersonType_AfterTheReveal_LeavesVotesStatisticsAndRevisionAlone()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        _service.ForceShowVotes(people[0], "c1");
        var before = _rooms.Get(roomId)!;
        var revision = before.Revision;
        var revealed = before.Revealed!.Votes.ToDictionary(vote => vote.Key, vote => (vote.Value.Mark, vote.Value.PersonType));

        var response = _service.ChangePersonType(people[1], "c2", PersonTypes.Observer);

        var after = _rooms.Get(roomId)!;
        Assert.True(response.Data!.Changed);
        Assert.Equal(revision, after.Revision);
        Assert.Equal(revealed, after.Revealed!.Votes.ToDictionary(vote => vote.Key, vote => (vote.Value.Mark, vote.Value.PersonType)));
        Assert.Same(VotingFollowUp.None, response.Data.FollowUp);
        Assert.NotNull(response.Data.VoteStatus.Statistics);
        Assert.Equal(["5", "8"], response.Data.VoteStatus.Statistics!.All.Marks.Select(mark => mark.Mark));
    }

    [Fact]
    public void ChangePersonType_VoterTurnsObserver_KeepsTheStoredVoteAndHidesItFromTheStatus()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[2], "c3", "5");

        _service.ChangePersonType(people[2], "c3", PersonTypes.Observer);
        _service.ChangePersonType(people[2], "c3", PersonTypes.Developer);
        var restored = _service.ChangePersonType(people[1], "c2", PersonTypes.Tester);

        Assert.Equal("5", _rooms.Get(roomId)!.Votes[people[2]]);
        Assert.Contains(people[2], restored.Data!.VoteStatus.Votes.Keys);
    }

    [Fact]
    public void ChangePersonType_ARevealThatFollowsSeesTheNewType()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        _service.ChangePersonType(people[1], "c2", PersonTypes.Tester);

        _service.ForceShowVotes(people[0], "c1");

        var types = _rooms.Get(roomId)!.Revealed!.Votes.ToDictionary(vote => vote.Key, vote => vote.Value.PersonType);
        Assert.Equal(PersonTypes.Tester, types[people[1]]);
    }

    [Fact]
    public void ChangePersonType_ARevealBetweenTheTwoLocks_KeepsTheRevealAndDoesNotRestartAnything()
    {
        var interleaving = new InterleavingPersonRepository(_people);
        var service = RoomServiceFactory.Create(_rooms, interleaving);
        var (roomId, people) = RoomWith(5, TwoDevs);
        service.Vote(people[0], "c1", "5");
        // The first person update is the lookup of the caller's room; the reveal lands after the second one, the type change itself.
        interleaving.AfterNextUpdate = () => interleaving.AfterNextUpdate = () => service.ForceShowVotes(people[0], "c1");

        var response = service.ChangePersonType(people[1], "c2", PersonTypes.Observer);

        var room = _rooms.Get(roomId)!;
        Assert.True(response.IsSuccess);
        Assert.False(room.VotingEnabled);
        Assert.Equal(PersonTypes.Developer, room.Revealed!.Votes[people[0]].PersonType);
        Assert.Same(VotingFollowUp.None, response.Data!.FollowUp);
        Assert.False(room.CountdownRunning);
    }

    // UpdateConfiguration

    [Fact]
    public void UpdateConfiguration_Owner_ReplacesTheConfigurationAndReturnsTheCards()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration("t-shirt-sizes", 12, true, 4));

        var stored = _rooms.Get(roomId)!.Configuration;
        Assert.True(response.IsSuccess);
        Assert.Equal(("t-shirt-sizes", 12, true, 4), (stored.CardSet, stored.CountdownSeconds, stored.ShowFireworks, stored.MaxParticipants));
        Assert.Equal(("t-shirt-sizes", 12, true, 4), (response.Data!.Configuration.CardSet, response.Data.Configuration.CountdownSeconds, response.Data.Configuration.ShowFireworks, response.Data.Configuration.MaxParticipants));
        Assert.Equal("XS", response.Data.Cards[0].Value);
        Assert.Equal("Person 1", response.Data.UpdatedBy);
        Assert.Equal(roomId, response.Data.RoomId);
    }

    [Fact]
    public void UpdateConfiguration_NotTheOwner_FailsAndChangesNothing()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var revision = _rooms.Get(roomId)!.Revision;

        var response = _service.UpdateConfiguration(people[1], "c2", Configuration("t-shirt-sizes"));

        Assert.Equal(ErrorCodes.NotRoomOwner, response.Errors.Single().Code);
        Assert.Equal("linear", _rooms.Get(roomId)!.Configuration.CardSet);
        Assert.Equal(revision, _rooms.Get(roomId)!.Revision);
    }

    [Fact]
    public void UpdateConfiguration_UnknownConnection_FailsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var response = _service.UpdateConfiguration(people[0], "elsewhere", Configuration());

        Assert.Equal(ErrorCodes.NotInRoom, response.Errors.Single().Code);
    }

    [Fact]
    public void UpdateConfiguration_UnknownCardSet_FailsValidation()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration("nope"));

        Assert.Equal(ErrorCodes.ValidationFailed, response.Errors.Single().Code);
        Assert.Equal("linear", _rooms.Get(roomId)!.Configuration.CardSet);
    }

    [Fact]
    public void UpdateConfiguration_AfterAnOwnerTransfer_RefusesTheFormerOwner()
    {
        var (_, people) = RoomWith(5, TwoDevs);
        _service.TransferOwnership(people[0], "c1", people[1]);

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration());

        Assert.Equal(ErrorCodes.NotRoomOwner, response.Errors.Single().Code);
        Assert.True(_service.UpdateConfiguration(people[1], "c2", Configuration()).IsSuccess);
    }

    [Fact]
    public void UpdateConfiguration_ShownVotes_AreCleared()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.ForceShowVotes(people[0], "c1");

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration(fireworks: true));

        var room = _rooms.Get(roomId)!;
        Assert.True(response.Data!.VotesCleared);
        Assert.True(room.VotingEnabled);
        Assert.Empty(room.Votes);
        Assert.Null(room.Revealed);
        Assert.False(response.Data.VoteStatus.VotingFinished);
        Assert.Null(response.Data.VoteStatus.Statistics);
        Assert.Same(VotingFollowUp.None, response.Data.FollowUp);
    }

    [Fact]
    public void UpdateConfiguration_NewCardSetWhileVotingIsOpen_ClearsTheVotes()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration("t-shirt-sizes"));

        Assert.True(response.Data!.VotesCleared);
        Assert.Empty(_rooms.Get(roomId)!.Votes);
    }

    [Fact]
    public void UpdateConfiguration_SameCardSetWhileVotingIsOpen_KeepsTheVotes()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration(countdown: 8));

        Assert.False(response.Data!.VotesCleared);
        Assert.Equal("5", _rooms.Get(roomId)!.Votes[people[0]]);
    }

    [Fact]
    public void UpdateConfiguration_CancelsARunningCountdown()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");
        var countdown = _service.Vote(people[2], "c3", "5").Data!.FollowUp.Countdown!;

        _service.UpdateConfiguration(people[0], "c1", Configuration(countdown: 8));

        Assert.True(_rooms.Get(roomId)!.Revision > countdown.Revision);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, countdown.Revision).Outcome);
    }

    [Fact]
    public void UpdateConfiguration_OnACompleteRound_RestartsTheCountdownWithTheNewLength()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration(countdown: 8));

        var start = response.Data!.FollowUp.Countdown!;
        Assert.Equal(8, start.Seconds);
        Assert.Equal(_rooms.Get(roomId)!.Revision, start.Revision);
        Assert.Equal(8, _rooms.Get(roomId)!.CountdownRemaining);
    }

    [Fact]
    public void UpdateConfiguration_ToCountdownZeroOnACompleteRound_RevealsAtOnce()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");

        var response = _service.UpdateConfiguration(people[0], "c1", Configuration(countdown: 0));

        Assert.NotNull(response.Data!.FollowUp.Revealed);
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void UpdateConfiguration_LaterJoinerSeesTheNewConfigurationAndCards()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.UpdateConfiguration(people[0], "c1", Configuration("t-shirt-sizes", 9, true, 3));
        var late = AddPerson();

        var joined = _service.Join(late, "c9", roomId).Data!;

        Assert.Equal("t-shirt-sizes", joined.Room.Configuration.CardSet);
        Assert.Equal("XS", joined.Cards[0].Value);
    }

    [Fact]
    public void UpdateConfiguration_AValidMarkOfTheNewSetIsAccepted()
    {
        var (_, people) = RoomWith(5, TwoDevs);
        _service.UpdateConfiguration(people[0], "c1", Configuration("t-shirt-sizes"));

        Assert.True(_service.Vote(people[1], "c2", "XL").IsSuccess);
        Assert.False(_service.Vote(people[1], "c2", "5").IsSuccess);
    }

    // TransferOwnership

    [Fact]
    public void TransferOwnership_OwnerToMember_ChangesTheOwner()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.TransferOwnership(people[0], "c1", people[1]);

        Assert.True(response.IsSuccess);
        Assert.Equal(people[1], response.Data!.NewOwner!.Guid);
        Assert.Equal(roomId, response.Data.RoomId);
        Assert.Equal(people[1], _rooms.Get(roomId)!.OwnerGuid);
    }

    [Fact]
    public void TransferOwnership_NotTheOwner_Fails()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);

        var response = _service.TransferOwnership(people[1], "c2", people[2]);

        Assert.Equal(ErrorCodes.NotRoomOwner, response.Errors.Single().Code);
        Assert.Equal(people[0], _rooms.Get(roomId)!.OwnerGuid);
    }

    [Fact]
    public void TransferOwnership_ToSomeoneNotInTheRoom_FailsNotAMember()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var outsider = AddPerson();

        var response = _service.TransferOwnership(people[0], "c1", outsider);

        var error = response.Errors.Single();
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("targetPersonGuid", error.Metadata["field"]);
        Assert.Equal("not_a_member", error.Metadata["reason"]);
        Assert.Equal(people[0], _rooms.Get(roomId)!.OwnerGuid);
    }

    [Fact]
    public void TransferOwnership_ToTheCurrentOwner_IsANoOp()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.TransferOwnership(people[0], "c1", people[0]);

        Assert.True(response.IsSuccess);
        Assert.Null(response.Data!.NewOwner);
        Assert.Equal(people[0], _rooms.Get(roomId)!.OwnerGuid);
    }

    [Fact]
    public void TransferOwnership_UnknownConnection_FailsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var response = _service.TransferOwnership(people[0], "elsewhere", people[1]);

        Assert.Equal(ErrorCodes.NotInRoom, response.Errors.Single().Code);
    }

    [Fact]
    public void TransferOwnership_ThenTheNewOwnerLeaves_HandsTheRoomOnAsBefore()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.TransferOwnership(people[0], "c1", people[1]);

        var exit = _service.Exit(people[1], "c2");

        Assert.NotNull(exit.Data!.NewOwner);
        Assert.Equal(exit.Data.NewOwner!.Guid, _rooms.Get(roomId)!.OwnerGuid);
        Assert.NotEqual(people[1], _rooms.Get(roomId)!.OwnerGuid);
    }
}
