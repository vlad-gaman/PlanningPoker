using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

/// <summary>
/// Durable storage for live rooms. The only consumers are the room persistence classes of this layer; the room logic never
/// sees it, so another database can be added by implementing this interface and registering it in place of the SQLite one.
/// </summary>
public interface IRoomStore
{
    /// <summary>Every stored room. A store that cannot be read yields none instead of failing.</summary>
    IReadOnlyList<StoredRoom> LoadAll();

    /// <summary>Inserts the room or replaces the one stored under the same id.</summary>
    void Save(StoredRoom room);

    void Delete(string roomId);
}
