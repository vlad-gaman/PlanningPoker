using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class UpdateMeRequestValidationTests
{
    private readonly UpdateMeRequestValidation _validation = new();

    [Fact]
    public void Validate_ValidRequest_Succeeds()
    {
        Assert.True(_validation.Validate(new UpdateMeRequest { Name = "Ann", PersonType = "dev" }).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("123456789012345678901")]
    public void Validate_BadName_FailsWithFieldName(string? name)
    {
        var response = _validation.Validate(new UpdateMeRequest { Name = name!, PersonType = "dev" });

        var error = Assert.Single(response.Errors);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("name", error.Metadata["field"]);
        Assert.True(error.Metadata.ContainsKey("reason"));
    }

    [Fact]
    public void Validate_NameAtBounds_Succeeds()
    {
        Assert.True(_validation.Validate(new UpdateMeRequest { Name = "Al", PersonType = "obs" }).IsSuccess);
        Assert.True(_validation.Validate(new UpdateMeRequest { Name = new string('a', 20), PersonType = "test" }).IsSuccess);
    }

    [Fact]
    public void Validate_BadPersonType_FailsWithFieldName()
    {
        var response = _validation.Validate(new UpdateMeRequest { Name = "Ann", PersonType = "boss" });

        var error = Assert.Single(response.Errors);
        Assert.Equal("personType", error.Metadata["field"]);
    }

    [Fact]
    public void Validate_BothBad_ReportsBothErrors()
    {
        var response = _validation.Validate(new UpdateMeRequest { Name = "", PersonType = "boss" });

        Assert.Equal(2, response.Errors.Count);
    }
}
