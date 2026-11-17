using Microsoft.AspNetCore.Hosting;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

/// <summary>An <see cref="ApiFactory"/> with a chosen environment and metrics scrape token (none when null).</summary>
public class MetricsApiFactory(string? token, string environment = "Production") : ApiFactory
{
    public const string ScrapeToken = "test-scrape-token";

    protected override string EnvironmentName => environment;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        if (token is not null)
        {
            builder.UseSetting($"{MetricsConstants.ConfigurationSection}:ScrapeToken", token);
        }
    }
}
