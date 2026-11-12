namespace PlanningPoker.Service.Services;

public class ServerStatistics(TimeProvider time) : IServerStatistics
{
    private static readonly TimeSpan Memory = TimeSpan.FromHours(24);

    private readonly Lock _gate = new();
    private readonly List<(DateTimeOffset At, int Rooms)> _sweeps = [];
    private int _restored;

    public DateTimeOffset StartedAt { get; } = time.GetUtcNow();

    public int RoomsRestoredAtStart
    {
        get
        {
            lock (_gate)
            {
                return _restored;
            }
        }
    }

    public void RecordRestored(int rooms)
    {
        lock (_gate)
        {
            _restored = rooms;
        }
    }

    public void RecordSweep(int rooms, int people)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            _sweeps.RemoveAll(sweep => now - sweep.At > Memory);
            if (rooms > 0)
            {
                _sweeps.Add((now, rooms));
            }
        }
    }

    public int RoomsSweptSince(DateTimeOffset since)
    {
        lock (_gate)
        {
            return _sweeps.Where(sweep => sweep.At >= since).Sum(sweep => sweep.Rooms);
        }
    }
}
