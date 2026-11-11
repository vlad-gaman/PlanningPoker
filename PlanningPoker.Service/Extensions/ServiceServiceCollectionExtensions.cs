using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        return services;
    }

    /// <summary>Loads the stored rooms into memory. Call it once after the host is built and before it starts, so people reconnect to the rooms they left.</summary>
    public static IServiceProvider RestoreRooms(this IServiceProvider services)
    {
        services.GetRequiredService<IRoomRestorer>().Restore();
        return services;
    }
}
