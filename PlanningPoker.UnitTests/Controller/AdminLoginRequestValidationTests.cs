using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class AdminLoginRequestValidationTests
{
    private readonly AdminLoginRequestValidation _validation = new();

    [Fact]
    public void Validate_BothFieldsPresent_Succeeds()
    {
        Assert.True(_validation.Validate(new AdminLoginRequest { Username = "operator", Password = "secret" }).IsSuccess);
    }

    [Fact]
    public void Validate_MissingFields_ReportsEachOne()
    {
        var result = _validation.Validate(new AdminLoginRequest { Username = "", Password = null! });

        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Equal(ErrorCodes.ValidationFailed, error.Code));
        Assert.Equal(["username", "password"], result.Errors.Select(error => error.Metadata["field"]));
    }

    [Fact]
    public void Validate_AnOverlongPassword_IsTooLong()
    {
        var result = _validation.Validate(new AdminLoginRequest
        {
            Username = "operator",
            Password = new string('x', AdminLoginRequestValidation.MaxLength + 1)
        });

        Assert.Equal("too_long", result.Errors.Single().Metadata["reason"]);
    }
}

public class AdminSessionResponseMapperTests
{
    private readonly AdminSessionResponseMapper _mapper = new();

    [Fact]
    public void MapSignedIn_CarriesTheUsernameAndToken()
    {
        var response = _mapper.MapSignedIn("operator", "token");

        Assert.True(response.Authenticated);
        Assert.Equal("operator", response.Username);
        Assert.Equal("token", response.CsrfToken);
    }

    [Fact]
    public void MapSignedOut_CarriesNothingElse()
    {
        var response = _mapper.MapSignedOut();

        Assert.False(response.Authenticated);
        Assert.Null(response.Username);
        Assert.Null(response.CsrfToken);
    }
}
