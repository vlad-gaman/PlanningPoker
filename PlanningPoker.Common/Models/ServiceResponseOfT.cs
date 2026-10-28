namespace PlanningPoker.Common.Models;

public class ServiceResponse<T> : ServiceResponse
{
    public T? Data { get; init; }

    public static ServiceResponse<T> Success(T data)
    {
        return new ServiceResponse<T> { Data = data };
    }

    public static new ServiceResponse<T> Failure(params ServiceError[] errors)
    {
        return new ServiceResponse<T> { Errors = errors };
    }

    public static ServiceResponse<T> Failure(ServiceResponse failed)
    {
        return new ServiceResponse<T> { Errors = failed.Errors };
    }
}
