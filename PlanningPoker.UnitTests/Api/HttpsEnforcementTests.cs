using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Api;

/// <summary>An Operator-enabled host in a chosen environment with the <c>Https:Redirect</c> setting chosen (null leaves it unset).</summary>
public class HttpsApiFactory(string environment, bool? redirect) : AdminApiFactory
{
    protected override string EnvironmentName => environment;

    protected override bool? HttpsRedirect => redirect;

    /// <summary>A client that follows nothing, keeps no cookies and speaks plain HTTP, like the App Service front end passing a request on.</summary>
    public HttpClient CreateRaw()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
    }
}

public class HttpsEnforcementTests
{
    private const string Credentials = """{"username":"operator","password":"correct horse battery staple"}""";

    private static HttpRequestMessage Get(string path, bool behindTls = false, string host = "localhost")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = host;
        if (behindTls)
        {
            request.Headers.Add("X-Forwarded-Proto", "https");
        }

        return request;
    }

    private static HttpRequestMessage Login(string password, bool behindTls, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/login")
        {
            Content = new StringContent(Credentials.Replace("correct horse battery staple", password), System.Text.Encoding.UTF8, "application/json")
        };
        if (behindTls)
        {
            request.Headers.Add("X-Forwarded-Proto", "https");
        }

        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return request;
    }

    [Theory]
    [InlineData("/api/admin/session")]
    [InlineData("/api/admin/overview")]
    [InlineData("/admin")]
    [InlineData("/admin/sign-in")]
    public async Task PlainHttp_ToTheManagementPage_IsRedirectedToHttps(string path)
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Get(path));

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal($"https://localhost{path}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PlainHttp_Login_NeverReachesTheLoginHandler()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Login(AdminApiFactory.Password, behindTls: false));

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var issues = factory.Services.GetRequiredService<IIssueLog>().Recent(50);
        Assert.DoesNotContain(issues, issue => issue.Message.Contains("signed in") || issue.Message.Contains("sign-in"));
    }

    [Fact]
    public async Task OutsideDevelopment_TheRedirectIsOnUnlessSwitchedOff()
    {
        using var factory = new HttpsApiFactory("Production", redirect: null);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session"));

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
    }

    [Fact]
    public async Task BehindTlsTermination_TheRequestIsSeenAsHttpsAndReachesTheHandler()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session", behindTls: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BehindTlsTermination_TheOperatorCookiesAreSecure()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Login(AdminApiFactory.Password, behindTls: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").Select(header => header.ToLowerInvariant()).ToList();
        var session = Assert.Single(cookies, cookie => cookie.StartsWith($"{OperatorConstants.CookieName}="));
        Assert.Contains("secure", session);
        Assert.Contains("httponly", session);
        Assert.Contains("samesite=strict", session);
        var xsrf = Assert.Single(cookies, cookie => cookie.StartsWith($"{OperatorConstants.XsrfCookieName.ToLowerInvariant()}="));
        Assert.Contains("secure", xsrf);
    }

    [Fact]
    public async Task OutsideDevelopment_TheOperatorCookieIsSecureEvenWhenTheRequestLooksPlain()
    {
        using var factory = new HttpsApiFactory("Production", redirect: false);

        var response = await factory.CreateRaw().SendAsync(Login(AdminApiFactory.Password, behindTls: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = response.Headers.GetValues("Set-Cookie").Single(header => header.StartsWith($"{OperatorConstants.CookieName}=")).ToLowerInvariant();
        Assert.Contains("secure", session);
    }

    [Fact]
    public async Task SwitchedOff_PlainHttpIsServedAsBefore()
    {
        using var factory = new HttpsApiFactory("Production", redirect: false);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Development_KeepsWorkingOverPlainHttp()
    {
        using var factory = new HttpsApiFactory("Development", redirect: null);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Development_CanStillSignInOverPlainHttp()
    {
        using var factory = new HttpsApiFactory("Development", redirect: null);

        var response = await factory.CreateRaw().SendAsync(Login(AdminApiFactory.Password, behindTls: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith($"{OperatorConstants.CookieName}="));
    }

    [Fact]
    public async Task OverHttps_OutsideDevelopment_HstsIsSentWithAShortMaxAge()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session", behindTls: true, host: "poker.example.com"));

        Assert.Equal("max-age=604800", response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task OverPlainHttp_NoHstsIsSent()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session", host: "poker.example.com"));

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task OverHttps_InDevelopment_NoHstsIsSent()
    {
        using var factory = new HttpsApiFactory("Development", redirect: null);

        var response = await factory.CreateRaw().SendAsync(Get("/api/admin/session", behindTls: true, host: "poker.example.com"));

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task TheForwardedClientAddress_IsWhatTheSignInLogAndLockoutSee()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        await factory.CreateRaw().SendAsync(Login("wrong password", behindTls: true, forwardedFor: "203.0.113.50"));

        var failed = factory.Services.GetRequiredService<IIssueLog>().Recent(50).First(issue => issue.IpAddress is not null);
        Assert.Equal("203.0.113.50", failed.IpAddress);
    }

    [Fact]
    public async Task OnlyTheLastForwardedAddressIsTrusted_SoAClientCannotInventItsOwn()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);

        await factory.CreateRaw().SendAsync(Login("wrong password", behindTls: true, forwardedFor: "198.51.100.1, 203.0.113.50"));

        var failed = factory.Services.GetRequiredService<IIssueLog>().Recent(50).First(issue => issue.IpAddress is not null);
        Assert.Equal("203.0.113.50", failed.IpAddress);
    }

    [Fact]
    public async Task ASignInOverHttps_StillWorksEndToEnd()
    {
        using var factory = new HttpsApiFactory("Production", redirect: true);
        var client = factory.CreateBrowser();

        var response = await client.PostAsJsonAsync("/api/admin/login", new { username = AdminApiFactory.Username, password = AdminApiFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class HttpsOptionsTests
{
    [Theory]
    [InlineData(null, "Production", true)]
    [InlineData(null, "Staging", true)]
    [InlineData(null, "Development", false)]
    [InlineData(false, "Production", false)]
    [InlineData(true, "Development", true)]
    [InlineData(true, "Production", true)]
    [InlineData(false, "Development", false)]
    public void ShouldRedirect_FollowsTheSettingAndOtherwiseTheEnvironment(bool? setting, string environment, bool expected)
    {
        var options = new HttpsOptions { Redirect = setting };

        Assert.Equal(expected, HttpsEnforcementExtensions.ShouldRedirect(options, new FakeHostEnvironment(environment)));
    }

    [Fact]
    public void Defaults_RedirectToPort443_WithAWeekOfHsts()
    {
        var options = new HttpsOptions();

        Assert.Null(options.Redirect);
        Assert.Equal(443, options.RedirectPort);
        Assert.Equal(TimeSpan.FromDays(7), options.HstsMaxAge);
    }
}
