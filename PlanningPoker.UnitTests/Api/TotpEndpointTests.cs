using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using PlanningPoker.Common.Constants;
using PlanningPoker.UnitTests.Service;

namespace PlanningPoker.UnitTests.Api;

/// <summary>An <see cref="AdminApiFactory"/> whose Operator also has an authenticator secret.</summary>
public class TotpAdminApiFactory : AdminApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting($"{OperatorConstants.ConfigurationSection}:TotpSecret", TestTotp.Secret);
    }
}

public class TotpEndpointTests(TotpAdminApiFactory factory) : IClassFixture<TotpAdminApiFactory>
{
    private static Task<HttpResponseMessage> PasswordStep(HttpClient client, string password = AdminApiFactory.Password)
    {
        return client.PostAsJsonAsync("/api/admin/login", new { username = AdminApiFactory.Username, password });
    }

    private static Task<HttpResponseMessage> CodeStep(HttpClient client, string? challenge, string code)
    {
        return client.PostAsJsonAsync("/api/admin/login/totp", new { challenge, code });
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        return (await Body(response)).GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    private static async Task<string> Challenge(HttpClient client)
    {
        var response = await PasswordStep(client);
        return (await Body(response)).GetProperty("errors")[0].GetProperty("metadata").GetProperty("challenge").GetString()!;
    }

    private static async Task<bool> IsSignedIn(HttpClient client)
    {
        var body = await Body(await client.GetAsync("/api/admin/session"));
        return body.GetProperty("data").GetProperty("authenticated").GetBoolean();
    }

    [Fact]
    public async Task PasswordStep_AsksForTheCode_AndDoesNotSignIn()
    {
        var client = factory.CreateBrowser();

        var response = await PasswordStep(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminTotpRequired, await ErrorCode(response));
        Assert.DoesNotContain(response.Headers, header => header.Key == "Set-Cookie");
        Assert.False(await IsSignedIn(client));
    }

    [Fact]
    public async Task CodeStep_ACorrectCode_SignsIn()
    {
        using var own = new TotpAdminApiFactory();
        var client = own.CreateBrowser();
        var challenge = await Challenge(client);

        var response = await CodeStep(client, challenge, TestTotp.CodeAt(own.Time.GetUtcNow()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), header => header.StartsWith($"{OperatorConstants.CookieName}="));
        Assert.False(string.IsNullOrEmpty((await Body(response)).GetProperty("data").GetProperty("csrfToken").GetString()));
        Assert.True(await IsSignedIn(client));
    }

    [Fact]
    public async Task CodeStep_AWrongCode_Returns401AndStaysSignedOut()
    {
        using var own = new TotpAdminApiFactory();
        var client = own.CreateBrowser();
        var challenge = await Challenge(client);

        var response = await CodeStep(client, challenge, TestTotp.CodeAt(own.Time.GetUtcNow().AddMinutes(10)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminTotpInvalid, await ErrorCode(response));
        Assert.False(await IsSignedIn(client));
    }

    [Fact]
    public async Task CodeStep_AReusedCode_IsRefusedOnASecondSignIn()
    {
        using var own = new TotpAdminApiFactory();
        var code = TestTotp.CodeAt(own.Time.GetUtcNow());
        var first = own.CreateBrowser();
        await CodeStep(first, await Challenge(first), code);
        var second = own.CreateBrowser();

        var response = await CodeStep(second, await Challenge(second), code);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminTotpInvalid, await ErrorCode(response));
    }

    [Fact]
    public async Task CodeStep_AnExpiredChallenge_AsksToStartAgain()
    {
        using var own = new TotpAdminApiFactory();
        var client = own.CreateBrowser();
        var challenge = await Challenge(client);
        own.Time.Advance(OperatorConstants.ChallengeLifetime + TimeSpan.FromSeconds(1));

        var response = await CodeStep(client, challenge, TestTotp.CodeAt(own.Time.GetUtcNow()));

        Assert.Equal(ErrorCodes.AdminLoginExpired, await ErrorCode(response));
    }

    [Fact]
    public async Task CodeStep_AMadeUpChallenge_IsRefused()
    {
        var response = await CodeStep(factory.CreateBrowser(), "made.up", "123456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminLoginExpired, await ErrorCode(response));
    }

    [Fact]
    public async Task CodeStep_MissingFields_Returns400()
    {
        var response = await CodeStep(factory.CreateBrowser(), "", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CodeStep_WrongCodesLockSignIn()
    {
        using var own = new TotpAdminApiFactory();
        var client = own.CreateBrowser();
        var challenge = await Challenge(client);
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await CodeStep(client, challenge, "000000");
        }

        var response = await CodeStep(client, challenge, TestTotp.CodeAt(own.Time.GetUtcNow()));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminLoginLocked, await ErrorCode(response));
    }
}
