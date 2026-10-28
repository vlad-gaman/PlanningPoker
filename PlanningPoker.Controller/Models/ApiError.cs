namespace PlanningPoker.Controller.Models;

public class ApiError
{
    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public Dictionary<string, string> Metadata { get; init; } = default!;
}
