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

    /// <summary>The single definition of the error returned when a person is not a member of the room.</summary>
    public static ServiceError PersonNotFound(string roomId, Guid personId)
    {
        return Create(ErrorCodes.PersonNotFound, "The person is not in the room.", ("room_id", roomId), ("person_id", personId.ToString()));
    }

    /// <summary>The single definition of the error returned when a connection is not a member of any room.</summary>
    public static ServiceError NotInRoom()
    {
        return Create(ErrorCodes.NotInRoom, "The connection is not in a room.");
    }

    /// <summary>The single definition of the error returned when an observer tries to vote.</summary>
    public static ServiceError ObserverCannotVote()
    {
        return Create(ErrorCodes.ObserverCannotVote, "Observers cannot vote.");
    }

    /// <summary>The single definition of the error returned when a vote arrives after the votes were revealed.</summary>
    public static ServiceError VotingClosed()
    {
        return Create(ErrorCodes.VotingClosed, "Voting is closed until the votes are cleared.");
    }

    /// <summary>The single definition of the error returned when a non-owner uses an owner-only command.</summary>
    public static ServiceError NotRoomOwner()
    {
        return Create(ErrorCodes.NotRoomOwner, "Only the room owner can do this.");
    }

    /// <summary>The single definition of a field-level validation failure.</summary>
    public static ServiceError ValidationFailed(string field, string reason)
    {
        return Create(ErrorCodes.ValidationFailed, $"The field '{field}' is invalid.", ("field", field), ("reason", reason));
    }

    /// <summary>The single definition of the error returned for a failed Operator sign-in; the same for an unknown user and a wrong password.</summary>
    public static ServiceError AdminInvalidCredentials()
    {
        return Create(ErrorCodes.AdminInvalidCredentials, "The username or password is incorrect.");
    }

    /// <summary>The single definition of the error returned while sign-in is locked after repeated failures.</summary>
    public static ServiceError AdminLoginLocked()
    {
        return Create(ErrorCodes.AdminLoginLocked, "Too many failed sign-in attempts. Try again later.");
    }

    /// <summary>The single definition of the error returned after a correct password when the code step is still to come. Carries the challenge for it.</summary>
    public static ServiceError AdminTotpRequired(string challenge)
    {
        return Create(ErrorCodes.AdminTotpRequired, "Enter the code from your authenticator app.", ("challenge", challenge));
    }

    /// <summary>The single definition of the error returned for a wrong, reused or malformed authenticator code.</summary>
    public static ServiceError AdminTotpInvalid()
    {
        return Create(ErrorCodes.AdminTotpInvalid, "The code is incorrect.");
    }

    /// <summary>The single definition of the error returned when the password step is too old or not genuine, so sign-in starts again.</summary>
    public static ServiceError AdminLoginExpired()
    {
        return Create(ErrorCodes.AdminLoginExpired, "The sign-in step expired. Start again.");
    }

    /// <summary>The single definition of the error returned when a state-changing admin request lacks a valid anti-forgery token.</summary>
    public static ServiceError AdminAntiforgeryInvalid()
    {
        return Create(ErrorCodes.AdminAntiforgeryInvalid, "The anti-forgery token is missing or invalid.");
    }

    /// <summary>The single definition of the error returned when a signed-in caller may not use an endpoint.</summary>
    public static ServiceError Forbidden()
    {
        return Create(ErrorCodes.Forbidden, "You may not do this.");
    }

    /// <summary>The single definition of the error returned when an endpoint needs the Operator and there is no valid sign-in.</summary>
    public static ServiceError Unauthorized()
    {
        return Create(ErrorCodes.Unauthorized, "Sign in is required.");
    }
}
