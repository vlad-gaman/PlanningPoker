using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Models;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.Service.Extensions;

public static class ServiceServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddSingleton<IPersonMapper, PersonMapper>();
        services.AddSingleton<IPersonEntityMapper, PersonEntityMapper>();
        services.AddSingleton<IPersonService, PersonService>();
        services.AddSingleton<IRoomConfigurationMapper, RoomConfigurationMapper>();
        services.AddSingleton<IRoomConfigurationEntityMapper, RoomConfigurationEntityMapper>();
        services.AddSingleton<IRoomEntityMapper, RoomEntityMapper>();
        services.AddSingleton<IRoomMapper, RoomMapper>();
        services.AddSingleton<IVoteStatisticsMapper, VoteStatisticsMapper>();
        services.AddSingleton<IRevealedVotesEntityMapper, RevealedVotesEntityMapper>();
        services.AddSingleton<IVoteStatusMapper, VoteStatusMapper>();
        services.AddSingleton<IJoinRoomResultMapper, JoinRoomResultMapper>();
        services.AddSingleton<IExitRoomResultMapper, ExitRoomResultMapper>();
        services.AddSingleton<RoomIdentifierValidation>();
        services.AddSingleton<RoomExistsValidation>();
        services.AddSingleton<VoteMarkValidation>();
        services.AddSingleton<IWordListLoader, WordListLoader>();
        services.AddSingleton<IRoomNameGenerator>(provider => new RoomNameGenerator(provider.GetRequiredService<IWordListLoader>().Load()));
        services.AddSingleton<ICardSetService, CardSetService>();
        services.AddSingleton<IPlanningPokerMetrics, PlanningPokerMetrics>();
        services.AddSingleton<IRoomService, RoomService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRoomTimerService, RoomTimerService>();
        services.AddOptions<SweepOptions>().BindConfiguration(SweepConstants.ConfigurationSection);
        services.AddSingleton<ISweepService, SweepService>();
        services.AddHostedService<SweepBackgroundService>();
        services.AddOptions<PresenceOptions>().BindConfiguration(PresenceConstants.ConfigurationSection);
        services.AddSingleton<IPresenceService, PresenceService>();
        services.AddHostedService<PresenceBackgroundService>();
        services.AddSingleton<IRoomRestorer, RoomRestorer>();
        services.AddOptions<OperatorOptions>().BindConfiguration(OperatorConstants.ConfigurationSection);
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        services.AddSingleton<ILoginLockoutService, LoginLockoutService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ITotpChallengeService, TotpChallengeService>();
        services.AddSingleton<IOperatorAuthService, OperatorAuthService>();
        services.AddOptions<IssueLogOptions>().BindConfiguration(IssueLogConstants.ConfigurationSection);
        services.AddSingleton<IIssueEntryMapper, IssueEntryMapper>();
        services.AddSingleton<IIssueRedactor, IssueRedactor>();
        services.AddSingleton<IIssueLog, IssueLog>();
        services.AddSingleton<IServerStatistics, ServerStatistics>();
        services.AddSingleton<IAdminRoomMapper, AdminRoomMapper>();
        services.AddSingleton<IAdminOverviewMapper, AdminOverviewMapper>();
        services.AddSingleton<IAdminOverviewService, AdminOverviewService>();
        services.AddSingleton<ILoggerProvider, IssueLoggerProvider>();
        return services;
    }

    /// <summary>Loads the stored rooms into memory. Call it once after the host is built and before it starts, so people reconnect to the rooms they left.</summary>
    public static IServiceProvider RestoreRooms(this IServiceProvider services)
    {
        var restored = services.GetRequiredService<IRoomRestorer>().Restore();
        services.GetRequiredService<IServerStatistics>().RecordRestored(restored);
        if (restored > 0)
        {
            services.GetRequiredService<IIssueLog>().Record(IssueLevel.Info, "Startup", $"Restored {restored} rooms from the store");
        }

        return services;
    }
}
