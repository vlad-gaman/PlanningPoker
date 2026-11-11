using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using PlanningPoker.Controller.Models;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Controllers;
using PlanningPoker.Controller.Hubs;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Middleware;
using PlanningPoker.Controller.Services;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Extensions;

public static class ControllerLayerExtensions
{
    public static IServiceCollection AddControllerLayer(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(MeController).Assembly);
        services.AddSignalR();

        services.AddSingleton<IApiErrorMapper, ApiErrorMapper>();
        services.AddSingleton<IApiResponseMapper, ApiResponseMapper>();
        services.AddSingleton<IPersonResponseMapper, PersonResponseMapper>();
        services.AddSingleton<IApiResponseWriter, ApiResponseWriter>();
        services.AddSingleton<ISessionCookieService, SessionCookieService>();
        services.AddSingleton<ICardSetResponseMapper, CardSetResponseMapper>();
        services.AddSingleton<IRoomConfigurationRequestMapper, RoomConfigurationRequestMapper>();
        services.AddSingleton<IRoomSettingsMapper, RoomSettingsMapper>();
        services.AddSingleton<IRoomCreatedResponseMapper, RoomCreatedResponseMapper>();
        services.AddSingleton<IRoomExistsResponseMapper, RoomExistsResponseMapper>();
        services.AddSingleton<IRoomConfigurationResponseMapper, RoomConfigurationResponseMapper>();
        services.AddSingleton<IMarkShareResponseMapper, MarkShareResponseMapper>();
        services.AddSingleton<IGroupStatisticsResponseMapper, GroupStatisticsResponseMapper>();
        services.AddSingleton<IVoteStatisticsResponseMapper, VoteStatisticsResponseMapper>();
        services.AddSingleton<IVoteStatusResponseMapper, VoteStatusResponseMapper>();
        services.AddSingleton<IJoinRoomResponseMapper, JoinRoomResponseMapper>();
        services.AddSingleton<IPersonJoinedEventMapper, PersonJoinedEventMapper>();
        services.AddSingleton<IPersonExitedEventMapper, PersonExitedEventMapper>();
        services.AddSingleton<IPersonOffgridEventMapper, PersonOffgridEventMapper>();
        services.AddSingleton<IPersonBackEventMapper, PersonBackEventMapper>();
        services.AddSingleton<IOwnershipTransferredEventMapper, OwnershipTransferredEventMapper>();
        services.AddSingleton<IRoomDisposedEventMapper, RoomDisposedEventMapper>();
        services.AddSingleton<IRoomCountdownEventMapper, RoomCountdownEventMapper>();
        services.AddSingleton<IVoteCastEventMapper, VoteCastEventMapper>();
        services.AddSingleton<IVotesClearedEventMapper, VotesClearedEventMapper>();
        services.AddSingleton<IPersonTypeChangedEventMapper, PersonTypeChangedEventMapper>();
        services.AddSingleton<IRoomConfigurationUpdatedEventMapper, RoomConfigurationUpdatedEventMapper>();
        services.AddSingleton<ICardResponseMapper, CardResponseMapper>();
        services.AddSingleton<RoomEventPublisher>();
        services.AddSingleton<IRoomEventPublisher>(provider => provider.GetRequiredService<RoomEventPublisher>());
        services.AddSingleton<IRoomCountdownNotifier>(provider => provider.GetRequiredService<RoomEventPublisher>());
        services.AddSingleton<IRoomPresenceNotifier>(provider => provider.GetRequiredService<RoomEventPublisher>());
        services.AddSingleton<UpdateMeRequestValidation>();
        services.AddSingleton<RoomConfigurationValidation>();
        services.AddSingleton<CreateRoomRequestValidation>();
        services.AddSingleton<PersonTypeValidation>();
        services.AddSingleton<TransferTargetValidation>();
        services.AddOptions<MetricsOptions>().BindConfiguration(MetricsConstants.ConfigurationSection);
        services.AddSingleton<MetricsTokenValidation>();
        services.AddOpenTelemetry().WithMetrics(metrics =>
        {
            metrics.ConfigureResource(resource => resource.AddService(MetricsConstants.ServiceName));
            metrics.AddMeter(MetricsConstants.MeterName);
            metrics.AddMeter(MetricsConstants.BuiltInMeters.ToArray());
            metrics.AddPrometheusExporter(options => options.ScrapeResponseCacheDurationMilliseconds = 0);
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ApiBehaviorOptions>, ApiBehaviorOptionsSetup>());
        return services;
    }

    public static IApplicationBuilder UseControllerLayer(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<JsonOnlyMiddleware>();
        app.UseMiddleware<SessionPersonMiddleware>();
        return app;
    }

    public static IEndpointRouteBuilder MapControllerLayer(this IEndpointRouteBuilder endpoints)
    {
        // Created here so the gauges exist before the first request.
        endpoints.ServiceProvider.GetRequiredService<IPlanningPokerMetrics>();
        if (endpoints.ServiceProvider.GetRequiredService<MetricsTokenValidation>().IsFailClosed)
        {
            endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ControllerLayerExtensions))
                .LogWarning("{Setting} is not set, so {Route} answers 401 to every request.", $"{MetricsConstants.ConfigurationSection}:ScrapeToken", RoutePrefixes.Metrics);
        }

        // Prometheus text outside /api, so it is exempt from the "every REST endpoint is in Swagger" rule.
        endpoints.MapPrometheusScrapingEndpoint(RoutePrefixes.Metrics, null, builder => builder.UseMiddleware<MetricsTokenMiddleware>(), null)
            .DisableHttpMetrics();
        endpoints.MapControllers();
        endpoints.MapHub<RoomHub>(RoutePrefixes.RoomHub);
        endpoints.MapFallback($"{RoutePrefixes.Api}/{{**rest}}", WriteNotFound);
        endpoints.MapFallback($"{RoutePrefixes.Hubs}/{{**rest}}", WriteNotFound);
        return endpoints;
    }

    private static Task WriteNotFound(HttpContext context)
    {
        var writer = context.RequestServices.GetRequiredService<IApiResponseWriter>();
        return writer.WriteAsync(context, ServiceResponse.Failure(
            ServiceError.Create(ErrorCodes.NotFound, "The requested resource does not exist.")));
    }
}
