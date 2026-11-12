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
        services.AddSingleton<IPersistenceStatus, PersistenceStatus>();
        services.AddSingleton<IRoomStore, SqliteRoomStore>();
        services.AddSingleton<IIssueStore, SqliteIssueStore>();
        services.AddHostedService<RoomPersistenceWriter>();
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IRoomRepository, RoomRepository>();
        return services;
    }
}
