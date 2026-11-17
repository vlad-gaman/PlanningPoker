using PlanningPoker.Common.Constants;

namespace PlanningPoker.Repository.Models;

public class PersistenceOptions
{
    /// <summary>The SQLite connection string of the room store.</summary>
    public string ConnectionString { get; set; } = PersistenceConstants.ConnectionString;

    /// <summary>How often a room with someone connected is saved again.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = PersistenceConstants.HeartbeatInterval;
}
