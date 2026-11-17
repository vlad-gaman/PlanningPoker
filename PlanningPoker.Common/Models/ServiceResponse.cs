namespace PlanningPoker.Common.Models;

public class ServiceResponse
{
    public IReadOnlyList<ServiceError> Errors { get; init; } = [];

    public bool IsSuccess => Errors.Count == 0;

    public static ServiceResponse Success()
    {
        return new ServiceResponse();
    }

    public static ServiceResponse Failure(params ServiceError[] errors)
    {
        return new ServiceResponse { Errors = errors };
    }
}
