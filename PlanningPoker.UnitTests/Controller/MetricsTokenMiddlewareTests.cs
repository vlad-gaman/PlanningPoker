using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Middleware;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Controller.Validations;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class MetricsTokenMiddlewareTests
{
    private static MetricsTokenMiddleware Create(RequestDelegate next, string? token)
    {
        var validation = new MetricsTokenValidation(
            Options.Create(new MetricsOptions { ScrapeToken = token }),
            new FakeHostEnvironment());
        return new MetricsTokenMiddleware(next, validation, new ApiResponseWriter(new ApiResponseMapper(new ApiErrorMapper())));
    }

    private static DefaultHttpContext NewContext(string? authorization)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        return context;
    }

    [Fact]
    public async Task Invoke_WrongToken_Returns401WithChallengeAndErrorBody()
    {
        var called = false;
        var middleware = Create(_ => { called = true; return Task.CompletedTask; }, "tok");
        var context = NewContext("Bearer nope");

        await middleware.InvokeAsync(context);

        Assert.False(called);
        Assert.Equal(401, context.Response.StatusCode);
        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate.ToString());
        context.Response.Body.Position = 0;
        var body = JsonDocument.Parse(context.Response.Body).RootElement;
        Assert.Equal(ErrorCodes.Unauthorized, body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Invoke_CorrectToken_CallsNext()
    {
        var called = false;
        var middleware = Create(_ => { called = true; return Task.CompletedTask; }, "tok");
        var context = NewContext("Bearer tok");

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
    }
}
