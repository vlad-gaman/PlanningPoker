using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

/// <summary>The round timer stamp: set at creation and reset only by a reveal or a clear.</summary>
public class RoomServiceRoundTimerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;
    private readonly DateTimeOffset _created;

    public RoomServiceRoundTimerTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people, time: _time);
        _created = _time.GetUtcNow();
    }

    private Guid AddPerson(string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    private (string RoomId, Guid[] People) RoomWith(int countdown, int members = 2)
    {
        var people = Enumerable.Range(0, members).Select(_ => AddPerson()).ToArray();
        var settings = new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = countdown, ShowFireworks = false, MaxParticipants = 9 }
        };
        var roomId = _service.Create(people[0], settings).Data!.Id;
        for (var i = 0; i < people.Length; i++)
        {
            Assert.True(_service.Join(people[i], $"c{i + 1}", roomId).IsSuccess);
        }

        return (roomId, people);
    }

    private DateTimeOffset StartOf(string roomId) => _rooms.Get(roomId)!.RoundStartedAt;

    [Fact]
    public void Create_StartsTheRoundAtCreation_AndTheJoinSnapshotCarriesIt()
    {
        var (roomId, _) = RoomWith(5);
        _time.Advance(TimeSpan.FromSeconds(30));

        var joined = _service.Join(AddPerson(), "late", roomId);

        Assert.Equal(_created, StartOf(roomId));
        Assert.Equal(_created, joined.Data!.VoteStatus.RoundStartedAt);
    }

    [Fact]
    public void VotesJoinsExitsTypeAndSettingsChanges_DoNotResetIt()
    {
        var (roomId, people) = RoomWith(5, 3);
        _time.Advance(TimeSpan.FromSeconds(10));

        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[0], "c1", "8");
        _service.ChangePersonType(people[1], "c2", PersonTypes.Observer);
        _service.Join(AddPerson(), "c9", roomId);
        _service.Exit(people[2], "c3");
        _service.UpdateConfiguration(people[0], "c1", new RoomConfiguration { CardSet = "linear", CountdownSeconds = 3, ShowFireworks = false, MaxParticipants = 9 });

        Assert.Equal(_created, StartOf(roomId));
    }

    [Fact]
    public void ForceShowVotes_ResetsIt_AndAnAlreadyRevealedCallDoesNot()
    {
        var (roomId, people) = RoomWith(5);
        _time.Advance(TimeSpan.FromSeconds(10));
        var revealedAt = _time.GetUtcNow();

        var first = _service.ForceShowVotes(people[0], "c1");
        _time.Advance(TimeSpan.FromSeconds(5));
        _service.ForceShowVotes(people[0], "c1");

        Assert.Equal(revealedAt, first.Data!.VoteStatus!.RoundStartedAt);
        Assert.Equal(revealedAt, StartOf(roomId));
    }

    [Fact]
    public void ClearVotes_ResetsIt_AndReturnsTheNewStamp()
    {
        var (roomId, people) = RoomWith(5);
        _time.Advance(TimeSpan.FromSeconds(10));

        var cleared = _service.ClearVotes(people[0], "c1");

        Assert.Equal(_created.AddSeconds(10), cleared.Data!.RoundStartedAt);
        Assert.Equal(cleared.Data.RoundStartedAt, StartOf(roomId));
    }

    [Fact]
    public void TheCountdown_KeepsTheRoundRunning_AndTheRevealAtItsEndResetsIt()
    {
        var (roomId, people) = RoomWith(2);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;
        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(_created, StartOf(roomId));

        _service.Tick(roomId, start.Revision);
        _time.Advance(TimeSpan.FromSeconds(1));
        var revealed = _service.Tick(roomId, start.Revision);

        Assert.Equal(TickOutcome.Revealed, revealed.Outcome);
        Assert.Equal(_created.AddSeconds(2), revealed.Revealed!.RoundStartedAt);
    }

    [Fact]
    public void AnImmediateReveal_WithNoCountdown_ResetsIt()
    {
        var (roomId, people) = RoomWith(0);
        _service.Vote(people[0], "c1", "5");
        _time.Advance(TimeSpan.FromSeconds(7));

        var result = _service.Vote(people[1], "c2", "8");

        Assert.Equal(_created.AddSeconds(7), result.Data!.FollowUp.Revealed!.RoundStartedAt);
        Assert.Equal(_created.AddSeconds(7), StartOf(roomId));
    }
}
