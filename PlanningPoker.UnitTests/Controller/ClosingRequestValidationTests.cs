using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class ClosingRequestValidationTests
{
    private readonly ClosingRequestValidation _validation = new();

    [Fact]
    public void Validate_WithAConnectionId_Succeeds()
    {
        Assert.True(_validation.Validate(new ClosingRequest { ConnectionId = "abc" }).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutAConnectionId_FailsWithFieldName(string? connectionId)
    {
        var response = _validation.Validate(new ClosingRequest { ConnectionId = connectionId! });

        var error = Assert.Single(response.Errors);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("connectionId", error.Metadata["field"]);
    }

    [Fact]
    public void Validate_NoRequest_Fails()
    {
        Assert.False(_validation.Validate(null).IsSuccess);
    }
}
