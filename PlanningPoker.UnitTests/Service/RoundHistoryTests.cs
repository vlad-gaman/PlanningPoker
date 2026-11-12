using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Builders;

namespace PlanningPoker.UnitTests.Service;

public class RoundHistoryTests
{
    private static readonly DateTimeOffset At = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] TwoDevs = [PersonTypes.Developer, PersonTypes.Developer];

    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;

    public RoundHistoryTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people);
    }

    private Guid AddPerson(string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    private (string RoomId, Guid[] People) RoomWith(int countdown, params string[] types)
    {
        var people = types.Select(AddPerson).ToArray();
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

    private IReadOnlyList<RoundResultEntity> History(string roomId) => _rooms.Get(roomId)!.RoundHistory;

    private void PlayRound(string roomId, Guid[] people, params string[] marks)
    {
        for (var i = 0; i < marks.Length; i++)
        {
            Assert.True(_service.Vote(people[i], $"c{i + 1}", marks[i]).IsSuccess);
        }

        Assert.True(_service.ForceShowVotes(people[0], "c1").IsSuccess);
    }

    [Fact]
    public void ANewRoom_HasNoHistory()
    {
        var (roomId, _) = RoomWith(5, TwoDevs);

        Assert.Empty(History(roomId));
        Assert.Equal(0, _rooms.Get(roomId)!.RoundsRevealed);
    }

    [Fact]
    public void ARevealedRound_AddsOneEntryWithItsNumberAndTheAverageOfItsVotes()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        PlayRound(roomId, people, "3", "5");

        var entry = Assert.Single(History(roomId));
        Assert.Equal(1, entry.Round);
        Assert.Equal(4m, entry.AverageMark);
        Assert.NotEqual(default, entry.RevealedAt);
    }

    [Fact]
    public void TheAverageIsRoundedToTwoDecimals()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer);

        PlayRound(roomId, people, "1", "2", "2");

        Assert.Equal(1.67m, Assert.Single(History(roomId)).AverageMark);
    }

    [Fact]
    public void ARoundWithNoNumericVotes_HasNoAverage()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        PlayRound(roomId, people, "coffee", "?");

        Assert.Null(Assert.Single(History(roomId)).AverageMark);
    }

    [Fact]
    public void NonNumericVotesAreLeftOutOfTheAverage()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        PlayRound(roomId, people, "8", "?");

        Assert.Equal(8m, Assert.Single(History(roomId)).AverageMark);
    }

    [Fact]
    public void AReveal_WithNobodyHavingVoted_AddsAnEntryWithoutAnAverage()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        _service.ForceShowVotes(people[0], "c1");

        Assert.Null(Assert.Single(History(roomId)).AverageMark);
    }

    [Fact]
    public void ARoundClearedWithoutBeingRevealed_AddsNoEntry()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        _service.ClearVotes(people[0], "c1");

        Assert.Empty(History(roomId));
        Assert.Equal(0, _rooms.Get(roomId)!.RoundsRevealed);
    }

    [Fact]
    public void Clearing_AfterAReveal_KeepsTheHistory()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");

        _service.ClearVotes(people[0], "c1");

        Assert.Single(History(roomId));
    }

    [Fact]
    public void EachRevealedRound_GetsTheNextNumber()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");
        _service.ClearVotes(people[0], "c1");
        PlayRound(roomId, people, "8", "8");
        _service.ClearVotes(people[0], "c1");
        _service.Vote(people[0], "c1", "1");
        _service.ClearVotes(people[0], "c1");
        PlayRound(roomId, people, "10", "10");

        Assert.Equal([1, 2, 3], History(roomId).Select(entry => entry.Round));
        Assert.Equal([4m, 8m, 10m], History(roomId).Select(entry => entry.AverageMark!.Value));
    }

    [Fact]
    public void ARevealThatAlreadyHappened_IsNotAddedTwice()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");

        _service.ForceShowVotes(people[1], "c2");

        Assert.Single(History(roomId));
    }

    [Fact]
    public void ARevealAfterTheCountdown_IsRecorded()
    {
        var (roomId, people) = RoomWith(1, TwoDevs);
        _service.Vote(people[0], "c1", "2");
        var vote = _service.Vote(people[1], "c2", "4").Data!;

        var tick = _service.Tick(roomId, vote.FollowUp.Countdown!.Revision);

        Assert.Equal(TickOutcome.Revealed, tick.Outcome);
        var entry = Assert.Single(History(roomId));
        Assert.Equal(3m, entry.AverageMark);
    }

    [Fact]
    public void AnInstantReveal_WhenTheLastVoteComesIn_IsRecorded()
    {
        var (roomId, people) = RoomWith(0, TwoDevs);
        _service.Vote(people[0], "c1", "2");

        _service.Vote(people[1], "c2", "6");

        Assert.Equal(4m, Assert.Single(History(roomId)).AverageMark);
    }

    [Fact]
    public void TheHistory_KeepsOnlyTheLastFiftyRounds()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var rounds = RoomConstraints.RoundHistoryLimit + 3;
        for (var round = 1; round <= rounds; round++)
        {
            PlayRound(roomId, people, "5", "5");
            _service.ClearVotes(people[0], "c1");
        }

        var history = History(roomId);
        Assert.Equal(50, history.Count);
        Assert.Equal(4, history[0].Round);
        Assert.Equal(rounds, history[^1].Round);
        Assert.Equal(rounds, _rooms.Get(roomId)!.RoundsRevealed);
    }

    [Fact]
    public void TheRevealAnnouncement_CarriesTheHistoryNewestFirst()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");
        _service.ClearVotes(people[0], "c1");
        _service.Vote(people[0], "c1", "8");
        _service.Vote(people[1], "c2", "8");

        var revealed = _service.ForceShowVotes(people[0], "c1").Data!.VoteStatus!;

        Assert.Equal([2, 1], revealed.RoundHistory!.Select(result => result.Round));
        Assert.Equal([8m, 4m], revealed.RoundHistory!.Select(result => result.AverageMark!.Value));
    }

    [Fact]
    public void AVoteStatusWhileVotingIsOpen_CarriesNoHistory()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");
        _service.ClearVotes(people[0], "c1");

        var status = _service.Vote(people[0], "c1", "8").Data!.VoteStatus;

        Assert.Null(status.RoundHistory);
    }

    [Fact]
    public void AJoiner_GetsTheHistoryNewestFirst_EvenWhileVotingIsOpen()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");
        _service.ClearVotes(people[0], "c1");
        PlayRound(roomId, people, "8", "8");
        _service.ClearVotes(people[0], "c1");
        var late = AddPerson();

        var joined = _service.Join(late, "c-late", roomId).Data!;

        Assert.Equal([2, 1], joined.Room.RoundHistory.Select(result => result.Round));
        Assert.Null(joined.VoteStatus.RoundHistory);
    }

    [Fact]
    public void AReconnectingMember_SeesTheSameHistory()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        PlayRound(roomId, people, "3", "5");

        var again = _service.Join(people[1], "c2-again", roomId).Data!;

        Assert.Equal([1], again.Room.RoundHistory.Select(result => result.Round));
    }
}

public class RoundResultMapperTests
{
    private static readonly DateTimeOffset At = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static RevealedVotesEntity Votes(params string[] marks)
    {
        return new RevealedVotesEntity { Votes = marks.ToDictionary(_ => Guid.NewGuid(), mark => new RevealedVoteEntity { Mark = mark, PersonType = PersonTypes.Developer }) };
    }

    [Fact]
    public void Entity_AveragesTheNumericMarks()
    {
        var entry = new RoundResultEntityMapper().Map(4, Votes("1", "2", "4", "?"), At);

        Assert.Equal(4, entry.Round);
        Assert.Equal(2.33m, entry.AverageMark);
        Assert.Equal(At, entry.RevealedAt);
    }

    [Fact]
    public void Entity_AcceptsHalvesAndDecimals()
    {
        Assert.Equal(0.75m, new RoundResultEntityMapper().Map(1, Votes("0.5", "1"), At).AverageMark);
    }

    [Fact]
    public void Entity_NoNumericMarks_HasNoAverage()
    {
        Assert.Null(new RoundResultEntityMapper().Map(1, Votes("coffee", "?", "XL"), At).AverageMark);
    }

    [Fact]
    public void Entity_NoVotes_HasNoAverage()
    {
        Assert.Null(new RoundResultEntityMapper().Map(1, Votes(), At).AverageMark);
    }

    [Fact]
    public void NewestFirst_ReversesAndCopies()
    {
        var history = new List<RoundResultEntity>
        {
            new() { Round = 1, AverageMark = 3m, RevealedAt = At },
            new() { Round = 2, AverageMark = null, RevealedAt = At.AddMinutes(5) }
        };

        var mapped = new RoundResultMapper().MapNewestFirst(history);

        Assert.Equal([2, 1], mapped.Select(result => result.Round));
        Assert.Null(mapped[0].AverageMark);
        Assert.Equal(3m, mapped[1].AverageMark);
        Assert.Equal(At.AddMinutes(5), mapped[0].RevealedAt);
    }

    [Fact]
    public void NewestFirst_NoHistory_IsEmpty()
    {
        Assert.Empty(new RoundResultMapper().MapNewestFirst([]));
    }

    [Theory]
    [InlineData("5", 5)]
    [InlineData("0.5", 0.5)]
    [InlineData("-1", -1)]
    public void MarkValue_ReadsNumbers(string mark, double expected)
    {
        Assert.Equal((decimal)expected, MarkValue.TryParse(mark));
    }

    [Theory]
    [InlineData("?")]
    [InlineData("coffee")]
    [InlineData("XL")]
    [InlineData("")]
    [InlineData("1,5")]
    public void MarkValue_RefusesTheRest(string mark)
    {
        Assert.Null(MarkValue.TryParse(mark));
    }

    [Fact]
    public void RoomMapper_CarriesTheHistoryNewestFirst()
    {
        var entity = new RoomEntity
        {
            Id = "r",
            RoundHistory = [new RoundResultEntity { Round = 1, AverageMark = 2m }, new RoundResultEntity { Round = 2, AverageMark = 4m }]
        };

        var room = new RoomMapper(new RoomConfigurationMapper(), new RoundResultMapper()).Map(entity);

        Assert.Equal([2, 1], room.RoundHistory.Select(result => result.Round));
    }

    [Fact]
    public void VoteStatusMapper_OpenVoting_HasNoHistory_AndARevealedRoundHasIt()
    {
        var voter = new Person { Guid = Guid.NewGuid(), Name = "n", PersonType = PersonTypes.Developer };
        var room = new RoomEntity
        {
            VotingEnabled = true,
            Configuration = new RoomConfigurationEntity { CardSet = "linear" },
            RoundHistory = [new RoundResultEntity { Round = 1, AverageMark = 2m }]
        };

        Assert.Null(VoteStatusMappers.Create().Map(room, [voter]).RoundHistory);

        room.VotingEnabled = false;
        room.Revealed = new RevealedVotesEntity();

        Assert.Equal([1], VoteStatusMappers.Create().Map(room, [voter]).RoundHistory!.Select(result => result.Round));
    }

    [Fact]
    public void ResponseMappers_CarryTheHistoryToTheClient()
    {
        var results = new[] { new RoundResult { Round = 2, AverageMark = 4.5m, RevealedAt = At }, new RoundResult { Round = 1, AverageMark = null, RevealedAt = At } };

        var response = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = new Dictionary<Guid, string>(), RoundHistory = results });
        var open = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = new Dictionary<Guid, string>() });

        Assert.Equal([2, 1], response.RoundHistory!.Select(result => result.Round));
        Assert.Equal(4.5m, response.RoundHistory![0].AverageMark);
        Assert.Null(response.RoundHistory![1].AverageMark);
        Assert.Null(open.RoundHistory);
        Assert.Equal(1, new RoundResultResponseMapper().Map(results[1]).Round);
    }

    [Fact]
    public void RoomEntityCopy_CopiesTheHistoryAndTheCounter()
    {
        var source = new RoomEntity { Id = "r", RoundsRevealed = 7, RoundHistory = [new RoundResultEntity { Round = 7, AverageMark = 3m, RevealedAt = At }] };

        var copy = new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()).Map(source);
        source.RoundHistory[0].AverageMark = 99m;
        source.RoundHistory.Add(new RoundResultEntity { Round = 8 });

        Assert.Equal(7, copy.RoundsRevealed);
        var entry = Assert.Single(copy.RoundHistory);
        Assert.Equal(3m, entry.AverageMark);
        Assert.Equal(At, entry.RevealedAt);
    }

    [Fact]
    public void ARoomStoredWithoutAHistory_ReadsBackWithAnEmptyOne()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new RoomEntity { Id = "old" });
        var stripped = System.Text.RegularExpressions.Regex.Replace(json, "\"RoundsRevealed\":0,|\"RoundHistory\":\\[\\],?", string.Empty);

        var room = System.Text.Json.JsonSerializer.Deserialize<RoomEntity>(stripped)!;

        Assert.Empty(room.RoundHistory);
        Assert.Equal(0, room.RoundsRevealed);
    }

    [Fact]
    public void TheHistory_SurvivesTheStoredFormat()
    {
        var entity = new RoomEntity { Id = "r", RoundsRevealed = 2, RoundHistory = [new RoundResultEntity { Round = 2, AverageMark = 1.5m, RevealedAt = At }] };

        var back = System.Text.Json.JsonSerializer.Deserialize<RoomEntity>(System.Text.Json.JsonSerializer.Serialize(entity))!;

        Assert.Equal(2, back.RoundsRevealed);
        var entry = Assert.Single(back.RoundHistory);
        Assert.Equal(1.5m, entry.AverageMark);
        Assert.Equal(At, entry.RevealedAt);
    }
}
