namespace PlanningPoker.Common.Constants;

public static class ErrorCodes
{
    public const string InternalError = "internal_error";
    public const string ValidationFailed = "validation_failed";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string NoSession = "no_session";
    public const string Unauthorized = "unauthorized";
    public const string NotFound = "not_found";
    public const string RoomNotFound = "room_not_found";
    public const string PersonNotFound = "person_not_found";
    public const string RoomIdGenerationFailed = "room_id_generation_failed";
    public const string NotRoomOwner = "not_room_owner";
    public const string Forbidden = "forbidden";
    public const string AdminInvalidCredentials = "admin_invalid_credentials";
    public const string AdminLoginLocked = "admin_login_locked";
    public const string AdminAntiforgeryInvalid = "admin_antiforgery_invalid";
    public const string AdminTotpRequired = "admin_totp_required";
    public const string AdminTotpInvalid = "admin_totp_invalid";
    public const string AdminLoginExpired = "admin_login_expired";

    // Hub-only: returned from RoomHub methods, never from REST, so they have no entry in the REST status table.
    public const string NotInRoom = "not_in_room";
    public const string ConnectionClosed = "connection_closed";
    public const string ObserverCannotVote = "observer_cannot_vote";
    public const string VotingClosed = "voting_closed";
}
