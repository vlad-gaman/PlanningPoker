using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Service.Extensions;

public static class ServiceServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddSingleton<IPersonMapper, PersonMapper>();
        services.AddSingleton<IPersonEntityMapper, PersonEntityMapper>();
        services.AddSingleton<IPersonService, PersonService>();
        return services;
    }
}
