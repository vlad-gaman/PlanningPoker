using Microsoft.Extensions.Configuration;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.UnitTests.Controller;

public class RateLimitingOptionsTests
{
    [Fact]
    public void Defaults_AreTheDecidedLimitsPerMinute()
    {
        var options = new RateLimitingOptions();

        Assert.True(options.Enabled);
        Assert.Equal((10, TimeSpan.FromMinutes(1)), (options.MeUpdate.PermitLimit, options.MeUpdate.Window));
        Assert.Equal((10, TimeSpan.FromMinutes(1)), (options.RoomCreate.PermitLimit, options.RoomCreate.Window));
        Assert.Equal((30, TimeSpan.FromMinutes(1)), (options.RoomLookup.PermitLimit, options.RoomLookup.Window));
        Assert.Equal((60, TimeSpan.FromMinutes(1)), (options.PresenceClosing.PermitLimit, options.PresenceClosing.Window));
        Assert.Equal((10, TimeSpan.FromMinutes(1)), (options.FailedJoin.PermitLimit, options.FailedJoin.Window));
    }

    [Fact]
    public void Configuration_OverridesOneLimitAndKeepsTheOthers()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RateLimitingConstants.ConfigurationSection}:RoomCreate:PermitLimit"] = "3",
            [$"{RateLimitingConstants.ConfigurationSection}:RoomCreate:Window"] = "00:00:30"
        }).Build();

        var options = configuration.GetSection(RateLimitingConstants.ConfigurationSection).Get<RateLimitingOptions>()!;

        Assert.Equal((3, TimeSpan.FromSeconds(30)), (options.RoomCreate.PermitLimit, options.RoomCreate.Window));
        Assert.Equal(RateLimitingConstants.MeUpdateLimit, options.MeUpdate.PermitLimit);
        Assert.True(options.Enabled);
    }
}
