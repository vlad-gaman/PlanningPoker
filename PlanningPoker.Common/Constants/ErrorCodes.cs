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
    public const string PersonStillInRoom = "person_still_in_room";
    public const string ConnectionClosed = "connection_closed";
}
