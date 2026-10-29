using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.UnitTests.Service;

public class RoomValidationTests
{
    private static RoomRepository NewRooms() => new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper()));

    [Theory]
    [InlineData("room")]
    [InlineData("42_big_red-cat")]
    [InlineData("0b9b4f6e-3e2b-4a0e-9d44-5f1d3c1f6e11")]
    public void RoomIdentifier_AcceptsLettersDigitsDashAndUnderscore(string id)
    {
        Assert.True(new RoomIdentifierValidation().Validate(id).IsSuccess);
    }

    [Theory]
    [InlineData(null, "required")]
    [InlineData("", "required")]
    [InlineData("a b", "invalid_characters")]
    [InlineData("a<b", "invalid_characters")]
    [InlineData("ünï", "invalid_characters")]
    public void RoomIdentifier_RejectsEmptyAndForbiddenCharacters(string? id, string reason)
    {
        var error = Assert.Single(new RoomIdentifierValidation().Validate(id).Errors);

        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal(reason, error.Metadata["reason"]);
    }

    [Fact]
    public void RoomIdentifier_RejectsOverLongIds()
    {
        var error = Assert.Single(new RoomIdentifierValidation().Validate(new string('a', RoomConstraints.IdMaxLength + 1)).Errors);

        Assert.Equal("too_long", error.Metadata["reason"]);
    }

    [Fact]
    public void RoomExists_KnownRoom_Succeeds()
    {
        var rooms = NewRooms();
        rooms.TryAdd(new RoomEntity { Id = "r1" });

        Assert.True(new RoomExistsValidation(rooms).Validate("r1").IsSuccess);
    }

    [Fact]
    public void RoomExists_UnknownRoom_ReturnsRoomNotFoundWithRoomId()
    {
        var error = Assert.Single(new RoomExistsValidation(NewRooms()).Validate("r2").Errors);

        Assert.Equal(ErrorCodes.RoomNotFound, error.Code);
        Assert.Equal("r2", error.Metadata["room_id"]);
    }
}
