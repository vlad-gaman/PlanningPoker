using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;

namespace PlanningPoker.Controller.Extensions;

/// <summary>Turns framework model-binding failures (for example malformed JSON) into <c>validation_failed</c> ApiResponses.</summary>
public class ApiBehaviorOptionsSetup(IApiResponseMapper mapper) : IConfigureOptions<ApiBehaviorOptions>
{
    public void Configure(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => ServiceError.Create(ErrorCodes.ValidationFailed, "The request is invalid.", ("field", entry.Key), ("reason", "invalid")))
                .DefaultIfEmpty(ServiceError.Create(ErrorCodes.ValidationFailed, "The request is invalid."))
                .ToArray();
            var response = ServiceResponse.Failure(errors);
            return new ObjectResult(mapper.Map(response)) { StatusCode = mapper.GetStatusCode(response) };
        };
    }
}
