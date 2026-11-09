using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class MetricsTokenValidationTests
{
    private const string Token = "s3cret-token";

    private static MetricsTokenValidation Create(string? token, string environment = "Production")
    {
        return new MetricsTokenValidation(
            Options.Create(new MetricsOptions { ScrapeToken = token }),
            new FakeHostEnvironment(environment));
    }

    [Theory]
    [InlineData("Bearer s3cret-token")]
    [InlineData("bearer s3cret-token")]
    [InlineData("BEARER s3cret-token")]
    public void Validate_CorrectToken_Succeeds(string header)
    {
        Assert.True(Create(Token).Validate(header).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer ")]
    [InlineData("Bearer wrong-token")]
    [InlineData("Bearer s3cret-toke")]
    [InlineData("Bearer s3cret-token-and-more")]
    [InlineData("Bearer x")]
    [InlineData("s3cret-token")]
    [InlineData("Basic s3cret-token")]
    [InlineData("Bearers3cret-token")]
    public void Validate_MissingOrWrongToken_ReturnsUnauthorized(string? header)
    {
        var response = Create(Token).Validate(header);

        Assert.False(response.IsSuccess);
        Assert.Equal(ErrorCodes.Unauthorized, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Validate_NoTokenInDevelopment_Succeeds()
    {
        Assert.True(Create(null, Environments.Development).Validate(null).IsSuccess);
        Assert.True(Create("", Environments.Development).Validate("Bearer anything").IsSuccess);
    }

    [Fact]
    public void Validate_TokenInDevelopment_IsEnforced()
    {
        var validation = Create(Token, Environments.Development);

        Assert.False(validation.Validate(null).IsSuccess);
        Assert.False(validation.Validate("Bearer wrong").IsSuccess);
        Assert.True(validation.Validate($"Bearer {Token}").IsSuccess);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Validate_NoTokenOutsideDevelopment_FailsClosed(string environment)
    {
        var validation = Create(null, environment);

        Assert.False(validation.Validate(null).IsSuccess);
        Assert.False(validation.Validate("Bearer ").IsSuccess);
        Assert.False(validation.Validate("Bearer anything").IsSuccess);
    }

    [Theory]
    [InlineData(null, "Production", true)]
    [InlineData("", "Staging", true)]
    [InlineData(null, "Development", false)]
    [InlineData(Token, "Production", false)]
    public void IsFailClosed_OnlyWithoutATokenOutsideDevelopment(string? token, string environment, bool expected)
    {
        Assert.Equal(expected, Create(token, environment).IsFailClosed);
    }
}
