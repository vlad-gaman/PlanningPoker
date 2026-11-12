namespace PlanningPoker.Service.Services;

/// <summary>Counters of this run of the server that nothing else keeps: when it started, what came back from the store, what the sweep removed.</summary>
public interface IServerStatistics
{
    DateTimeOffset StartedAt { get; }

    int RoomsRestoredAtStart { get; }

    void RecordRestored(int rooms);

    void RecordSweep(int rooms, int people);

    /// <summary>Rooms the sweep removed at or after the given time.</summary>
    int RoomsSweptSince(DateTimeOffset since);
}
