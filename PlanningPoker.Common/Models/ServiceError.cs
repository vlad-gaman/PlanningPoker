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
}
