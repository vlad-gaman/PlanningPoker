namespace PlanningPoker.UnitTests.Api;

// OpenTelemetry's MeterProvider subscribes to meters by name only, so a disposed host's same-named meter deactivates the shared stream in every other live host.
[CollectionDefinition("Metrics host", DisableParallelization = true)]
public class MetricsHostCollection;
