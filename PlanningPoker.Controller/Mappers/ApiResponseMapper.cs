using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class ApiResponseMapper(IApiErrorMapper errorMapper) : IApiResponseMapper
{
    public ApiResponse<T> Map<T>(ServiceResponse<T> response)
    {
        return new ApiResponse<T>
        {
            Data = response.IsSuccess ? response.Data : default,
            Errors = MapErrors(response)
        };
    }

    public ApiResponse<object> Map(ServiceResponse response)
    {
        return new ApiResponse<object> { Errors = MapErrors(response) };
    }

    public int GetStatusCode(ServiceResponse response, int successStatusCode = 200)
    {
        if (response.IsSuccess)
        {
            return successStatusCode;
        }

        return ErrorStatusCodes.Table.GetValueOrDefault(response.Errors[0].Code, ErrorStatusCodes.Fallback);
    }

    private List<ApiError>? MapErrors(ServiceResponse response)
    {
        return response.IsSuccess ? null : response.Errors.Select(errorMapper.Map).ToList();
    }
}
