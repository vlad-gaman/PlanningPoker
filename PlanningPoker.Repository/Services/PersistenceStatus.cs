using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Services;

public class PersistenceStatus : IPersistenceStatus
{
    private readonly Lock _gate = new();
    private PersistenceStatusSnapshot _snapshot = new();

    public void RecordWrite(DateTimeOffset at, int failedRooms)
    {
        lock (_gate)
        {
            _snapshot = new PersistenceStatusSnapshot { LastWriteAt = at, LastWriteSucceeded = failedRooms == 0, FailedRooms = failedRooms };
        }
    }

    public PersistenceStatusSnapshot Snapshot()
    {
        lock (_gate)
        {
            return _snapshot;
        }
    }
}
