using PlanningPoker.Common.Models;

namespace PlanningPoker.UnitTests.Common;

public class ServiceResponseTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var response = ServiceResponse.Success();

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void Failure_IsNotSuccessAndKeepsErrors()
    {
        var error = ServiceError.Create("some_code", "Some message", ("key", "value"));

        var response = ServiceResponse.Failure(error);

        Assert.False(response.IsSuccess);
        Assert.Same(error, Assert.Single(response.Errors));
        Assert.Equal("value", error.Metadata["key"]);
    }

    [Fact]
    public void GenericSuccess_CarriesData()
    {
        var response = ServiceResponse<int>.Success(42);

        Assert.True(response.IsSuccess);
        Assert.Equal(42, response.Data);
    }

    [Fact]
    public void GenericFailure_HasNoDataAndKeepsErrors()
    {
        var response = ServiceResponse<string>.Failure(ServiceError.Create("c", "m"));

        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.Single(response.Errors);
    }

    [Fact]
    public void GenericFailure_FromOtherResponse_CopiesErrors()
    {
        var other = ServiceResponse.Failure(ServiceError.Create("c", "m"));

        var response = ServiceResponse<string>.Failure(other);

        Assert.Equal(other.Errors, response.Errors);
    }

    [Theory]
    [InlineData("dev", true)]
    [InlineData("test", true)]
    [InlineData("obs", true)]
    [InlineData("admin", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PersonTypes_IsValid(string? value, bool expected)
    {
        Assert.Equal(expected, PlanningPoker.Common.Constants.PersonTypes.IsValid(value));
    }
}
