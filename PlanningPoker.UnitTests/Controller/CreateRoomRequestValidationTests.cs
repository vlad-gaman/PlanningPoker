using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class CreateRoomRequestValidationTests
{
    private static CreateRoomRequest Request(string cardSet = "linear", int countdown = 5, int max = 50)
    {
        return new CreateRoomRequest
        {
            UseFunRoomName = true,
            Configuration = new RoomConfigurationRequest { CardSet = cardSet, CountdownSeconds = countdown, ShowFireworks = true, MaxParticipants = max }
        };
    }

    private static List<string> Fields(CreateRoomRequest request)
    {
        var response = new CreateRoomRequestValidation().Validate(request);
        Assert.All(response.Errors, error => Assert.Equal(ErrorCodes.ValidationFailed, error.Code));
        return response.Errors.Select(error => error.Metadata["field"]).ToList();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void Validate_BoundaryCountdowns_Pass(int countdown)
    {
        Assert.True(new CreateRoomRequestValidation().Validate(Request(countdown: countdown)).IsSuccess);
    }

    [Fact]
    public void Validate_MissingConfiguration_Fails()
    {
        Assert.Equal(["configuration"], Fields(new CreateRoomRequest()));
    }

    [Fact]
    public void Validate_UnknownCardSet_Fails()
    {
        Assert.Equal(["configuration.cardSet"], Fields(Request(cardSet: "tarot")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validate_CountdownOutsideRange_Fails(int countdown)
    {
        Assert.Equal(["configuration.countdownSeconds"], Fields(Request(countdown: countdown)));
    }

    [Fact]
    public void Validate_MaxParticipantsBelowOne_Fails()
    {
        Assert.Equal(["configuration.maxParticipants"], Fields(Request(max: 0)));
    }

    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        Assert.Equal(3, Fields(Request(cardSet: "x", countdown: 0, max: 0)).Count);
    }
}
