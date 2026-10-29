namespace PlanningPoker.UnitTests.Fakes;

/// <summary>One broadcast captured by <see cref="FakeClientProxy"/>.</summary>
public sealed record SentMessage(string Target, IReadOnlyList<string> Excluded, string Method, object? Payload);
