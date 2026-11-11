using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace PlanningPoker.UnitTests.Api;

/// <summary>
/// A host on a given store file and a given fake clock, so that a test can stop one host and boot the next on the same
/// store, as a server restart does. The store file outlives the host; the test deletes it.
/// </summary>
public class RestartApiFactory(string storePath, FakeTimeProvider time) : ApiFactory
{
    public FakeTimeProvider Time { get; } = time;

    protected override string StorePath { get; } = storePath;

    protected override bool KeepStore => true;

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
