namespace PlanningPoker.Common.Constants;

public static class PersistenceConstants
{
    /// <summary>The appsettings section that overrides the persistence options.</summary>
    public const string ConfigurationSection = "Persistence";

    /// <summary>Where the rooms are stored unless configured otherwise: a SQLite file next to the app. On App Service point it at the persistent /home mount.</summary>
    public const string ConnectionString = "Data Source=planningpoker.db";

    /// <summary>How often a room with someone connected is saved again, so a restart can tell when its people were last seen online.</summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    /// <summary>The version of the stored room format. A store holding another version is set aside and the server starts with no rooms.</summary>
    public const int SchemaVersion = 1;
}
