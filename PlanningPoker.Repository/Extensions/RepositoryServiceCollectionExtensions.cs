using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Repository.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
    {
        services.AddSingleton<IRoomConfigurationEntityCopyMapper, RoomConfigurationEntityCopyMapper>();
        services.AddSingleton<IRoomEntityCopyMapper, RoomEntityCopyMapper>();
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IRoomRepository, RoomRepository>();
        return services;
    }
}
