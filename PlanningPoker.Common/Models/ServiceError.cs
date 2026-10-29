using PlanningPoker.Common.Constants;

namespace PlanningPoker.Common.Models;

public class ServiceError
{
    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = default!;

    public static ServiceError Create(string code, string message, params (string Key, string Value)[] metadata)
    {
        return new ServiceError
        {
            Code = code,
            Message = message,
            Metadata = metadata.ToDictionary(pair => pair.Key, pair => pair.Value)
        };
    }

    /// <summary>The single definition of the error returned when a request has no valid session person.</summary>
    public static ServiceError NoSession()
    {
        return Create(ErrorCodes.NoSession, "There is no valid session.");
    }

    /// <summary>The single definition of the error returned when a room id does not match a stored room.</summary>
    public static ServiceError RoomNotFound(string roomId)
    {
        return Create(ErrorCodes.RoomNotFound, "The room does not exist.", ("room_id", roomId));
    }

    /// <summary>The single definition of a field-level validation failure.</summary>
    public static ServiceError ValidationFailed(string field, string reason)
    {
        return Create(ErrorCodes.ValidationFailed, $"The field '{field}' is invalid.", ("field", field), ("reason", reason));
    }
}
