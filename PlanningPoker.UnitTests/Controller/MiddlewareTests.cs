using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Middleware;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.UnitTests.Controller;

public class MiddlewareTests
{
    private static ApiResponseWriter NewWriter() => new(new ApiResponseMapper(new ApiErrorMapper()));

    private static DefaultHttpContext NewContext(string method = "GET", string path = "/api/x", string? contentType = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.ContentType = contentType;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonElement ReadBody(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }

    [Fact]
    public async Task ExceptionHandling_ThrowingNext_Returns500InternalError()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"), NullLogger<ExceptionHandlingMiddleware>.Instance, NewWriter());
        var context = NewContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.Equal(ErrorCodes.InternalError, ReadBody(context).GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task ExceptionHandling_NoException_PassesThrough()
    {
        var middleware = new ExceptionHandlingMiddleware(ctx => { ctx.Response.StatusCode = 204; return Task.CompletedTask; }, NullLogger<ExceptionHandlingMiddleware>.Instance, NewWriter());
        var context = NewContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(204, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task JsonOnly_StateChangingWithoutJson_Returns415(string method)
    {
        var called = false;
        var middleware = new JsonOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, NewWriter());
        var context = NewContext(method, contentType: "application/x-www-form-urlencoded");

        await middleware.InvokeAsync(context);

        Assert.False(called);
        Assert.Equal(415, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.UnsupportedMediaType, ReadBody(context).GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task JsonOnly_MissingContentType_Returns415()
    {
        var middleware = new JsonOnlyMiddleware(_ => Task.CompletedTask, NewWriter());
        var context = NewContext("POST");

        await middleware.InvokeAsync(context);

        Assert.Equal(415, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    public async Task JsonOnly_JsonBody_PassesThrough(string contentType)
    {
        var called = false;
        var middleware = new JsonOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, NewWriter());

        await middleware.InvokeAsync(NewContext("POST", contentType: contentType));

        Assert.True(called);
    }

    [Fact]
    public async Task JsonOnly_GetRequest_PassesThrough()
    {
        var called = false;
        var middleware = new JsonOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, NewWriter());

        await middleware.InvokeAsync(NewContext("GET"));

        Assert.True(called);
    }

    [Fact]
    public async Task JsonOnly_NonApiPath_PassesThrough()
    {
        var called = false;
        var middleware = new JsonOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, NewWriter());

        await middleware.InvokeAsync(NewContext("POST", "/hubs/room/negotiate"));

        Assert.True(called);
    }
}
