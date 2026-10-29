using PlanningPoker.Common.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.UnitTests.Service;

public class VoteMarkValidationTests
{
    private static string Reason(ServiceResponse response)
    {
        Assert.False(response.IsSuccess);
        Assert.Equal("mark", response.Errors.Single().Metadata["field"]);
        return response.Errors.Single().Metadata["reason"];
    }

    private static ServiceResponse Validate(string? mark, string set = "linear")
    {
        return new VoteMarkValidation(new CardSetService()).Validate(set, mark);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("?")]
    [InlineData("coffee")]
    public void Validate_CardOfTheSet_Passes(string mark)
    {
        Assert.True(Validate(mark).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_Empty_Fails(string? mark)
    {
        Assert.Equal("required", Reason(Validate(mark)));
    }

    [Fact]
    public void Validate_TooLong_Fails()
    {
        Assert.Equal("too_long", Reason(Validate(new string('1', 21))));
    }

    [Theory]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("\"")]
    [InlineData("'")]
    [InlineData("&")]
    [InlineData("\0")]
    [InlineData("\r")]
    [InlineData("\n")]
    public void Validate_ForbiddenCharacter_Fails(string mark)
    {
        Assert.Equal("invalid_characters", Reason(Validate("1" + mark)));
    }

    [Fact]
    public void Validate_ReservedHide_Fails()
    {
        Assert.Equal("reserved", Reason(Validate("hide")));
    }

    [Fact]
    public void Validate_NotACardOfTheSet_Fails()
    {
        Assert.Equal("unknown_card", Reason(Validate("13")));
        Assert.Equal("unknown_card", Reason(Validate("green")));
    }
}
