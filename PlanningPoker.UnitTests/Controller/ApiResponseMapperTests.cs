using System.Text.Json;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;

namespace PlanningPoker.UnitTests.Controller;

public class ApiResponseMapperTests
{
    private readonly ApiResponseMapper _mapper = new(new ApiErrorMapper());

    [Theory]
    [InlineData(ErrorCodes.InternalError, 500)]
    [InlineData(ErrorCodes.ValidationFailed, 400)]
    [InlineData(ErrorCodes.UnsupportedMediaType, 415)]
    [InlineData(ErrorCodes.NoSession, 401)]
    [InlineData(ErrorCodes.NotFound, 404)]
    public void GetStatusCode_UsesStatusTable(string code, int expected)
    {
        var response = ServiceResponse.Failure(ServiceError.Create(code, "m"));

        Assert.Equal(expected, _mapper.GetStatusCode(response));
    }

    [Fact]
    public void GetStatusCode_UnmappedCode_Is500()
    {
        var response = ServiceResponse.Failure(ServiceError.Create("something_new", "m"));

        Assert.Equal(500, _mapper.GetStatusCode(response));
    }

    [Fact]
    public void GetStatusCode_Success_UsesGivenSuccessCode()
    {
        Assert.Equal(200, _mapper.GetStatusCode(ServiceResponse.Success()));
        Assert.Equal(201, _mapper.GetStatusCode(ServiceResponse.Success(), 201));
    }

    [Fact]
    public void Map_Success_HasDataAndNullErrors()
    {
        var api = _mapper.Map(ServiceResponse<string>.Success("hello"));

        Assert.Equal("hello", api.Data);
        Assert.Null(api.Errors);
    }

    [Fact]
    public void Map_Failure_HasErrorsAndNullData()
    {
        var response = ServiceResponse<string>.Failure(
            ServiceError.Create("person_not_found", "The person could not be found", ("person_id_1", "1"), ("person_id_2", "2")));

        var api = _mapper.Map(response);

        Assert.Null(api.Data);
        var error = Assert.Single(api.Errors!);
        Assert.Equal("person_not_found", error.Code);
        Assert.Equal("The person could not be found", error.Message);
        Assert.Equal("1", error.Metadata["person_id_1"]);
        Assert.Equal("2", error.Metadata["person_id_2"]);
    }

    [Fact]
    public void Map_NonGenericFailure_KeepsAllErrors()
    {
        var response = ServiceResponse.Failure(ServiceError.Create("a", "m"), ServiceError.Create("b", "m"));

        var api = _mapper.Map(response);

        Assert.Equal(2, api.Errors!.Count);
        Assert.Null(api.Data);
    }

    [Fact]
    public void Map_SerializesToDocumentedShape()
    {
        var api = _mapper.Map(ServiceResponse<string>.Failure(ServiceError.Create("c", "m", ("k", "v"))));

        var json = JsonSerializer.Serialize(api, JsonSerializerOptions.Web);

        Assert.Equal("""{"data":null,"errors":[{"code":"c","message":"m","metadata":{"k":"v"}}]}""", json);
    }

    [Fact]
    public void ApiErrorMapper_MapsFields()
    {
        var error = new ApiErrorMapper().Map(ServiceError.Create("c", "m", ("k", "v")));

        Assert.Equal("c", error.Code);
        Assert.Equal("m", error.Message);
        Assert.Equal("v", error.Metadata["k"]);
    }
}
