using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Api;

/// <summary>A fake-clock host with an Operator configured (or not), a hash generated at run time and no failure delay.</summary>
public class AdminApiFactory : FakeClockApiFactory
{
    public const string Username = "operator";
    public const string Password = "correct horse battery staple";

    protected virtual bool Configured => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting($"{OperatorConstants.ConfigurationSection}:FailureDelay", "00:00:00");
        if (Configured)
        {
            builder.UseSetting($"{OperatorConstants.ConfigurationSection}:Username", Username);
            builder.UseSetting($"{OperatorConstants.ConfigurationSection}:PasswordHash", new PasswordHashingService().Hash(Password));
        }
    }

    /// <summary>A browser-like client over HTTPS (the Operator cookie is Secure, so a plain-HTTP client would drop it) with its own cookie jar.</summary>
    public HttpClient CreateBrowser()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
    }
}

/// <summary>An <see cref="AdminApiFactory"/> with no Operator username or hash, in the chosen environment.</summary>
public class UnconfiguredAdminApiFactory(string environment) : AdminApiFactory
{
    protected override string EnvironmentName => environment;

    protected override bool Configured => false;
}
