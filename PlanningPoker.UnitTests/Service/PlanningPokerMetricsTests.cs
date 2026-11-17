using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class PlanningPokerMetricsTests
{
    private readonly IMeterFactory _meterFactory = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly PlanningPokerMetrics _metrics;

    public PlanningPokerMetricsTests()
    {
        _metrics = new PlanningPokerMetrics(_meterFactory, _rooms, _people);
    }

    private MetricCollector<T> Collect<T>(string instrument) where T : struct
    {
        return new MetricCollector<T>(_meterFactory, MetricsConstants.MeterName, instrument);
    }

    private static long Total(MetricCollector<long> collector, string? cardSet = null)
    {
        return collector.GetMeasurementSnapshot()
            .Where(m => cardSet is null || Equals(m.Tags[MetricsConstants.CardSetTag], cardSet))
            .Sum(m => m.Value);
    }

    private static RoomSettings Settings(string cardSet = "linear")
    {
        return new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = cardSet, CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 }
        };
    }

    private Guid AddPerson(string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    [Fact]
    public void RoomCreated_CountsOneRoom()
    {
        var created = Collect<long>(MetricsConstants.RoomsCreated);

        _metrics.RoomCreated();
        _metrics.RoomCreated();

        Assert.Equal(2, Total(created));
    }

    [Fact]
    public void VoteCast_TagsTheCardSet()
    {
        var votes = Collect<long>(MetricsConstants.VotesCast);

        _metrics.VoteCast("fibonacci");
        _metrics.VoteCast("fibonacci");
        _metrics.VoteCast("linear");

        Assert.Equal(2, Total(votes, "fibonacci"));
        Assert.Equal(1, Total(votes, "linear"));
    }

    [Fact]
    public void RoomsInMemory_ReturnsTheRepositoryCount()
    {
        var gauge = Collect<int>(MetricsConstants.RoomsInMemory);
        var service = RoomServiceFactory.Create(_rooms, _people, metrics: _metrics);
        service.Create(AddPerson(), Settings());
        service.Create(AddPerson(), Settings());

        gauge.RecordObservableInstruments();

        Assert.Equal(2, gauge.LastMeasurement!.Value);
    }

    [Fact]
    public void PeopleInMemory_ReturnsTheRepositoryCount()
    {
        var gauge = Collect<int>(MetricsConstants.PeopleInMemory);
        AddPerson();
        AddPerson();
        AddPerson();

        gauge.RecordObservableInstruments();

        Assert.Equal(3, gauge.LastMeasurement!.Value);
    }

    [Fact]
    public void RoomService_Create_CountsEachCreatedRoom()
    {
        var created = Collect<long>(MetricsConstants.RoomsCreated);
        var service = RoomServiceFactory.Create(_rooms, _people, metrics: _metrics);

        service.Create(AddPerson(), Settings());
        service.Create(AddPerson(), Settings());

        Assert.Equal(2, Total(created));
    }

    [Fact]
    public void RoomService_Vote_CountsEachSuccessfulVoteIncludingChanges()
    {
        var votes = Collect<long>(MetricsConstants.VotesCast);
        var service = RoomServiceFactory.Create(_rooms, _people, metrics: _metrics);
        var owner = AddPerson();
        var roomId = service.Create(owner, Settings("linear")).Data!.Id;
        service.Join(owner, "c1", roomId);

        service.Vote(owner, "c1", "3");
        service.Vote(owner, "c1", "5");

        Assert.Equal(2, Total(votes, "linear"));
    }

    [Fact]
    public void RoomService_Vote_RejectedVotesAreNotCounted()
    {
        var votes = Collect<long>(MetricsConstants.VotesCast);
        var service = RoomServiceFactory.Create(_rooms, _people, metrics: _metrics);
        var owner = AddPerson();
        var observer = AddPerson(PersonTypes.Observer);
        var roomId = service.Create(owner, Settings()).Data!.Id;
        service.Join(owner, "c1", roomId);
        service.Join(observer, "c2", roomId);

        service.Vote(owner, "c1", "not-a-card");
        service.Vote(observer, "c2", "3");
        service.Vote(owner, "unknown-connection", "3");

        Assert.Empty(votes.GetMeasurementSnapshot());
    }
}
