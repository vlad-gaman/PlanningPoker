using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace PlanningPoker.UnitTests.Api;

/// <summary>An <see cref="ApiFactory"/> whose server runs on a <see cref="FakeTimeProvider"/>, so a test can move the clock.</summary>
public class FakeClockApiFactory : ApiFactory
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
