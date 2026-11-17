using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Services;

/// <summary>Remembers how the last write of rooms to the store went, for the management page.</summary>
public interface IPersistenceStatus
{
    void RecordWrite(DateTimeOffset at, int failedRooms);

    PersistenceStatusSnapshot Snapshot();
}
