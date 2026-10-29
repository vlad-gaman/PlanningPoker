using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanningPoker.Service.Mappers;
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
        services.AddSingleton<IVoteStatusMapper, VoteStatusMapper>();
        services.AddSingleton<IJoinRoomResultMapper, JoinRoomResultMapper>();
        services.AddSingleton<IExitRoomResultMapper, ExitRoomResultMapper>();
        services.AddSingleton<RoomIdentifierValidation>();
        services.AddSingleton<RoomExistsValidation>();
        services.AddSingleton<VoteMarkValidation>();
        services.AddSingleton<IWordListLoader, WordListLoader>();
        services.AddSingleton<IRoomNameGenerator>(provider => new RoomNameGenerator(provider.GetRequiredService<IWordListLoader>().Load()));
        services.AddSingleton<ICardSetService, CardSetService>();
        services.AddSingleton<IRoomService, RoomService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRoomTimerService, RoomTimerService>();
        return services;
    }
}
