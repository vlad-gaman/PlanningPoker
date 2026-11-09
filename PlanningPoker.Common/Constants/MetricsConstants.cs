namespace PlanningPoker.Common.Constants;

public static class MetricsConstants
{
    /// <summary>The appsettings section bound to the metrics options.</summary>
    public const string ConfigurationSection = "Metrics";

    /// <summary>The name of the meter that holds the application metrics.</summary>
    public const string MeterName = "PlanningPoker";

    public const string RoomsInMemory = "planningpoker.rooms.in_memory";
    public const string PeopleInMemory = "planningpoker.people.in_memory";
    public const string RoomsCreated = "planningpoker.rooms.created";
    public const string VotesCast = "planningpoker.votes.cast";

    /// <summary>The tag on <see cref="VotesCast"/> that holds the card set of the room.</summary>
    public const string CardSetTag = "card_set";

    /// <summary>The service name reported in the metrics resource.</summary>
    public const string ServiceName = "PlanningPoker";

    /// <summary>The built-in meters exported next to the application meter.</summary>
    public static readonly IReadOnlyList<string> BuiltInMeters =
    [
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Http.Connections",
        "System.Runtime",
        "Microsoft.AspNetCore.Server.Kestrel"
    ];
}
