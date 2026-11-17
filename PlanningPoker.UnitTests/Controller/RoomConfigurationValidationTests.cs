using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class RoomConfigurationValidationTests
{
    private readonly RoomConfigurationValidation _validation = new();

    private static RoomConfigurationRequest Request(string cardSet = "linear", int countdown = 5, int max = 50)
    {
        return new RoomConfigurationRequest { CardSet = cardSet, CountdownSeconds = countdown, ShowFireworks = true, MaxParticipants = max };
    }

    private List<string> Fields(RoomConfigurationRequest? request, string prefix = "")
    {
        var response = _validation.Validate(request, prefix);
        Assert.All(response.Errors, error => Assert.Equal(ErrorCodes.ValidationFailed, error.Code));
        return response.Errors.Select(error => error.Metadata["field"]).ToList();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void Validate_BoundaryCountdowns_Pass(int countdown)
    {
        Assert.True(_validation.Validate(Request(countdown: countdown)).IsSuccess);
    }

    [Fact]
    public void Validate_Missing_FailsAsConfigurationWithoutAPrefix()
    {
        Assert.Equal(["configuration"], Fields(null));
    }

    [Fact]
    public void Validate_Missing_FailsAsThePrefixInsideALargerRequest()
    {
        Assert.Equal(["configuration"], Fields(null, "configuration."));
    }

    [Fact]
    public void Validate_UnknownCardSet_NamesTheField()
    {
        Assert.Equal(["cardSet"], Fields(Request(cardSet: "tarot")));
        Assert.Equal(["configuration.cardSet"], Fields(Request(cardSet: "tarot"), "configuration."));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void Validate_CountdownOutsideRange_Fails(int countdown)
    {
        Assert.Equal(["countdownSeconds"], Fields(Request(countdown: countdown)));
    }

    [Fact]
    public void Validate_MaxParticipantsBelowOne_Fails()
    {
        Assert.Equal(["maxParticipants"], Fields(Request(max: 0)));
    }

    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        Assert.Equal(3, Fields(Request(cardSet: "x", countdown: -1, max: 0)).Count);
    }
}
