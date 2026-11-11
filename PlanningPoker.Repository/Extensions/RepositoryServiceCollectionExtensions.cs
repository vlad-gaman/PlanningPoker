using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Services;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Repository.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
    {
        services.AddSingleton<IRoomConfigurationEntityCopyMapper, RoomConfigurationEntityCopyMapper>();
        services.AddSingleton<IRevealedVotesEntityCopyMapper, RevealedVotesEntityCopyMapper>();
        services.AddSingleton<IRoomEntityCopyMapper, RoomEntityCopyMapper>();
        services.AddSingleton<IStoredRoomMapper, StoredRoomMapper>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceConstants.ConfigurationSection);
        services.AddSingleton<IRoomChangeLog, RoomChangeLog>();
        services.AddSingleton<IRoomStore, SqliteRoomStore>();
        services.AddSingleton<IRoomRestorer, RoomRestorer>();
        services.AddHostedService<RoomPersistenceWriter>();
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IRoomRepository, RoomRepository>();
        return services;
    }

    /// <summary>Loads the stored rooms into memory. Call it once after the host is built and before it starts, so people reconnect to the rooms they left.</summary>
    public static IServiceProvider RestoreRooms(this IServiceProvider services)
    {
        services.GetRequiredService<IRoomRestorer>().Restore();
        return services;
    }
}
