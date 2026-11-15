namespace PlanningPoker.Common.Constants;

public static class RateLimitingConstants
{
    /// <summary>The appsettings section that overrides the rate limits.</summary>
    public const string ConfigurationSection = "RateLimiting";

    /// <summary>The limiter policy of <c>PUT /api/me</c>.</summary>
    public const string MeUpdatePolicy = "me-update";

    /// <summary>The limiter policy of <c>POST /api/rooms</c>.</summary>
    public const string RoomCreatePolicy = "room-create";

    /// <summary>The limiter policy of <c>GET /api/rooms/{id}</c>.</summary>
    public const string RoomLookupPolicy = "room-lookup";

    /// <summary>The partition used when a request carries no client address.</summary>
    public const string UnknownClient = "unknown";

    /// <summary>The length of every limiter window.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Requests per window that one address may make to <c>PUT /api/me</c>.</summary>
    public const int MeUpdateLimit = 10;

    /// <summary>Requests per window that one address may make to <c>POST /api/rooms</c>.</summary>
    public const int RoomCreateLimit = 10;

    /// <summary>Requests per window that one address may make to <c>GET /api/rooms/{id}</c>.</summary>
    public const int RoomLookupLimit = 30;

    /// <summary>Failed <c>JoinRoom</c> calls per window that one address may make before further joins are refused.</summary>
    public const int FailedJoinLimit = 10;
}
