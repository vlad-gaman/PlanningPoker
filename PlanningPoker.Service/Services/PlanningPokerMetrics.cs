using System.Diagnostics.Metrics;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Service.Services;

/// <summary>The application metrics. No room or person id is ever used as a tag.</summary>
public sealed class PlanningPokerMetrics : IPlanningPokerMetrics
{
    private readonly Counter<long> _roomsCreated;
    private readonly Counter<long> _votesCast;

    public PlanningPokerMetrics(IMeterFactory meterFactory, IRoomRepository rooms, IPersonRepository people)
    {
        var meter = meterFactory.Create(MetricsConstants.MeterName);
        meter.CreateObservableGauge(MetricsConstants.RoomsInMemory, () => rooms.Ids().Count);
        meter.CreateObservableGauge(MetricsConstants.PeopleInMemory, () => people.Guids().Count);
        _roomsCreated = meter.CreateCounter<long>(MetricsConstants.RoomsCreated);
        _votesCast = meter.CreateCounter<long>(MetricsConstants.VotesCast);
    }

    public void RoomCreated()
    {
        _roomsCreated.Add(1);
    }

    public void VoteCast(string cardSet)
    {
        _votesCast.Add(1, new KeyValuePair<string, object?>(MetricsConstants.CardSetTag, cardSet));
    }
}
