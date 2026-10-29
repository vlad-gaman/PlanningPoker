using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.UnitTests.Common;

public class ServiceErrorTests
{
    [Fact]
    public void NoSession_IsTheSingleNoSessionError()
    {
        var error = ServiceError.NoSession();

        Assert.Equal(ErrorCodes.NoSession, error.Code);
        Assert.Equal("There is no valid session.", error.Message);
        Assert.Empty(error.Metadata);
    }
}
