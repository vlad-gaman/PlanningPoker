namespace PlanningPoker.Common.Constants;

public static class SessionTokenConstants
{
    /// <summary>Random bytes in a session token. Base64url-encoded without padding they make <see cref="EncodedLength"/> characters.</summary>
    public const int ByteLength = 32;

    public const int EncodedLength = 43;
}
