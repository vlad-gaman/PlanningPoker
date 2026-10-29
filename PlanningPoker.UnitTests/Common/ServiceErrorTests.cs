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

    [Fact]
    public void NotInRoom_IsTheSingleNotInRoomError()
    {
        var error = ServiceError.NotInRoom();

        Assert.Equal(ErrorCodes.NotInRoom, error.Code);
        Assert.Equal("The connection is not in a room.", error.Message);
        Assert.Empty(error.Metadata);
    }

    [Fact]
    public void RoomNotFound_CarriesTheRoomId()
    {
        var error = ServiceError.RoomNotFound("r1");

        Assert.Equal(ErrorCodes.RoomNotFound, error.Code);
        Assert.Equal("The room does not exist.", error.Message);
        Assert.Equal("r1", error.Metadata["room_id"]);
    }

    [Fact]
    public void ValidationFailed_CarriesFieldAndReason()
    {
        var error = ServiceError.ValidationFailed("name", "too_short");

        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("The field 'name' is invalid.", error.Message);
        Assert.Equal("name", error.Metadata["field"]);
        Assert.Equal("too_short", error.Metadata["reason"]);
    }
}
