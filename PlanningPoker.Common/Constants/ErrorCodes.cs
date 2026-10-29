namespace PlanningPoker.Common.Constants;

public static class ErrorCodes
{
    public const string InternalError = "internal_error";
    public const string ValidationFailed = "validation_failed";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string NoSession = "no_session";
    public const string NotFound = "not_found";
    public const string RoomNotFound = "room_not_found";
    public const string RoomIdGenerationFailed = "room_id_generation_failed";
    public const string NotRoomOwner = "not_room_owner";

    // Hub-only: returned from RoomHub methods, never from REST, so they have no entry in the REST status table.
    public const string NotInRoom = "not_in_room";
    public const string ConnectionClosed = "connection_closed";
    public const string ObserverCannotVote = "observer_cannot_vote";
    public const string VotingClosed = "voting_closed";
}
