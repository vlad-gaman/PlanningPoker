using Microsoft.Extensions.Hosting;

namespace PlanningPoker.UnitTests.Api;

/// <summary>The host running as Development, where Swagger is served.</summary>
public class DevelopmentApiFactory : ApiFactory
{
    protected override string EnvironmentName => Environments.Development;
}
