using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<IRoomEntityMapper, RoomEntityMapper>();
        services.AddSingleton<IRoomMapper, RoomMapper>();
        services.AddSingleton<IVoteStatusMapper, VoteStatusMapper>();
        services.AddSingleton<IJoinRoomResultMapper, JoinRoomResultMapper>();
        services.AddSingleton<IExitRoomResultMapper, ExitRoomResultMapper>();
        services.AddSingleton<RoomIdentifierValidation>();
        services.AddSingleton<RoomExistsValidation>();
        services.AddSingleton<IWordListLoader, WordListLoader>();
        services.AddSingleton<IRoomNameGenerator>(provider => new RoomNameGenerator(provider.GetRequiredService<IWordListLoader>().Load()));
        services.AddSingleton<ICardSetService, CardSetService>();
        services.AddSingleton<IRoomService, RoomService>();
        return services;
    }
}
