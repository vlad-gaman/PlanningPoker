using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

public class AdminEndpointTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private static Task<HttpResponseMessage> Login(HttpClient client, string username = AdminApiFactory.Username, string password = AdminApiFactory.Password)
    {
        return client.PostAsJsonAsync("/api/admin/login", new { username, password });
    }

    private static HttpRequestMessage Logout(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/logout") { Content = JsonContent.Create(new { }) };
        if (token is not null)
        {
            request.Headers.Add(OperatorConstants.XsrfHeaderName, token);
        }

        return request;
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        return (await Body(response)).GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    private static async Task<bool> IsSignedIn(HttpClient client)
    {
        var body = await Body(await client.GetAsync("/api/admin/session"));
        return body.GetProperty("data").GetProperty("authenticated").GetBoolean();
    }

    [Fact]
    public async Task Logout_NotSignedIn_Returns401WithAnApiResponse()
    {
        var response = await factory.CreateBrowser().SendAsync(Logout(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.Unauthorized, await ErrorCode(response));
    }

    [Fact]
    public async Task Session_NotSignedIn_SaysSo()
    {
        var response = await factory.CreateBrowser().GetAsync("/api/admin/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await Body(response)).GetProperty("data").GetProperty("authenticated").GetBoolean());
    }

    [Theory]
    [InlineData("operator", "wrong password")]
    [InlineData("someone-else", AdminApiFactory.Password)]
    public async Task Login_WrongCredentials_Returns401WithTheSameCode(string username, string password)
    {
        var response = await Login(factory.CreateBrowser(), username, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminInvalidCredentials, await ErrorCode(response));
    }

    [Fact]
    public async Task Login_MissingFields_Returns400()
    {
        var response = await Login(factory.CreateBrowser(), "", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_SetsASecureHttpOnlyStrictCookieAndReturnsAToken()
    {
        var response = await Login(factory.CreateBrowser());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(header => header.StartsWith($"{OperatorConstants.CookieName}=")).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        var data = (await Body(response)).GetProperty("data");
        Assert.True(data.GetProperty("authenticated").GetBoolean());
        Assert.False(string.IsNullOrEmpty(data.GetProperty("csrfToken").GetString()));
    }

    [Fact]
    public async Task Logout_WithoutTheToken_Returns403AndKeepsTheSession()
    {
        var client = factory.CreateBrowser();
        await Login(client);

        var response = await client.SendAsync(Logout(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminAntiforgeryInvalid, await ErrorCode(response));
        Assert.True(await IsSignedIn(client));
    }

    [Fact]
    public async Task Logout_WithTheToken_EndsTheSession()
    {
        var client = factory.CreateBrowser();
        var token = (await Body(await Login(client))).GetProperty("data").GetProperty("csrfToken").GetString();

        var response = await client.SendAsync(Logout(token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await IsSignedIn(client));
    }

    [Fact]
    public async Task Session_SignedIn_ReissuesAWorkingToken()
    {
        var client = factory.CreateBrowser();
        await Login(client);
        var session = await Body(await client.GetAsync("/api/admin/session"));
        var token = session.GetProperty("data").GetProperty("csrfToken").GetString();

        var response = await client.SendAsync(Logout(token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Session_StaysOpenWhileUsed_AndExpiresAfterThirtyIdleMinutes()
    {
        using var own = new AdminApiFactory();
        var client = own.CreateBrowser();
        await Login(client);

        own.Time.Advance(TimeSpan.FromMinutes(20));
        Assert.True(await IsSignedIn(client));

        own.Time.Advance(TimeSpan.FromMinutes(20));
        Assert.True(await IsSignedIn(client));

        own.Time.Advance(TimeSpan.FromMinutes(31));
        Assert.False(await IsSignedIn(client));
    }

    [Fact]
    public async Task Login_AfterFiveFailures_IsLockedUntilTheLockoutEnds()
    {
        using var own = new AdminApiFactory();
        var client = own.CreateBrowser();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await Login(client, password: "wrong password");
        }

        var locked = await Login(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.Equal(ErrorCodes.AdminLoginLocked, await ErrorCode(locked));

        own.Time.Advance(OperatorConstants.LockoutDuration + TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await Login(client)).StatusCode);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task Login_NothingConfigured_IsRefusedInEveryEnvironment(string environment)
    {
        using var unconfigured = new UnconfiguredAdminApiFactory(environment);

        var response = await Login(unconfigured.CreateBrowser());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminInvalidCredentials, await ErrorCode(response));
    }
}
