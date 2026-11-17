namespace PlanningPoker.UnitTests.Api;

/// <summary>A host with the rate limits on and every limit at its default, as in Production.</summary>
public class RateLimitedApiFactory : ApiFactory
{
    protected override bool RateLimitingEnabled => true;
}
