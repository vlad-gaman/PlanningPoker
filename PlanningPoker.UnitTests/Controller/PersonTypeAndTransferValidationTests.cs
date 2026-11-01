using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.UnitTests.Controller;

public class PersonTypeAndTransferValidationTests
{
    [Theory]
    [InlineData("dev")]
    [InlineData("test")]
    [InlineData("obs")]
    public void PersonType_Valid_Passes(string personType)
    {
        Assert.True(new PersonTypeValidation().Validate(personType).IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DEV")]
    [InlineData("admin")]
    public void PersonType_Invalid_FailsOnThePersonTypeField(string? personType)
    {
        var error = Assert.Single(new PersonTypeValidation().Validate(personType).Errors);

        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("personType", error.Metadata["field"]);
        Assert.Equal("invalid_person_type", error.Metadata["reason"]);
    }

    [Fact]
    public void TransferTarget_Guid_IsParsed()
    {
        var guid = Guid.NewGuid();

        var response = new TransferTargetValidation().Validate(guid.ToString());

        Assert.True(response.IsSuccess);
        Assert.Equal(guid, response.Data);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void TransferTarget_NotAGuid_FailsInvalidGuid(string? target)
    {
        var error = Assert.Single(new TransferTargetValidation().Validate(target).Errors);

        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("targetPersonGuid", error.Metadata["field"]);
        Assert.Equal("invalid_guid", error.Metadata["reason"]);
    }
}
