using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Api;

public class IssueLogEndpointTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private IIssueLog Log => factory.Services.GetRequiredService<IIssueLog>();

    [Fact]
    public void AWarningLoggedAnywhereInTheApp_AppearsInTheIssuesLog()
    {
        var marker = "marker-" + Guid.NewGuid().ToString("N");

        factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PlanningPoker.Test.Somewhere").LogWarning("Something odd {Marker}", marker);

        var entry = Log.Recent(50).First(e => e.Message.Contains(marker));
        Assert.Equal(IssueLevel.Warning, entry.Level);
        Assert.Equal("Somewhere", entry.Source);
    }

    [Fact]
    public void RoutineInformation_IsNotInTheIssuesLog()
    {
        var marker = "routine-" + Guid.NewGuid().ToString("N");

        factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PlanningPoker.Test.Somewhere").LogInformation("Joined {Marker}", marker);

        Assert.DoesNotContain(Log.Recent(100), e => e.Message.Contains(marker));
    }

    [Fact]
    public async Task AFailedOperatorSignIn_IsRecordedWithTheAddressOnlyOnThatEntry()
    {
        await factory.CreateBrowser().PostAsJsonAsync("/api/admin/login", new { username = AdminApiFactory.Username, password = "wrong password" });

        var failed = Log.Recent(50).First(e => e.Source == "Admin" && e.Message.Contains("failed"));
        Assert.Equal(IssueLevel.Warning, failed.Level);
        Assert.All(Log.Recent(500).Where(e => e.Id != failed.Id && !e.Message.Contains("sign-in")), e => Assert.Null(e.IpAddress));
    }
}
