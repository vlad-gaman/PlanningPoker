namespace PlanningPoker.Controller.Models;

public class ApiResponse<T>
{
    public T? Data { get; init; }

    public List<ApiError>? Errors { get; init; }
}
