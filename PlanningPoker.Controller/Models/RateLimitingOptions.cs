using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Models;

/// <summary>
/// The per-address limits on the endpoints that anyone can call without signing in. Every limit is a fixed window per client
/// address (as read after the forwarded headers), so the defaults need no configuration. Hosts that make many requests from
/// one address on purpose (the end-to-end run, the integration tests) set <see cref="Enabled"/> to false.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>Whether the limits apply at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary><c>PUT /api/me</c>, which creates an anonymous person.</summary>
    public RateLimitRule MeUpdate { get; set; } = new() { PermitLimit = RateLimitingConstants.MeUpdateLimit };

    /// <summary><c>POST /api/rooms</c>.</summary>
    public RateLimitRule RoomCreate { get; set; } = new() { PermitLimit = RateLimitingConstants.RoomCreateLimit };

    /// <summary><c>GET /api/rooms/{id}</c>, which tells whether a room exists.</summary>
    public RateLimitRule RoomLookup { get; set; } = new() { PermitLimit = RateLimitingConstants.RoomLookupLimit };

    /// <summary>Failed <c>JoinRoom</c> calls on the hub; only failures count, so joining rooms that exist is never limited.</summary>
    public RateLimitRule FailedJoin { get; set; } = new() { PermitLimit = RateLimitingConstants.FailedJoinLimit };
}
