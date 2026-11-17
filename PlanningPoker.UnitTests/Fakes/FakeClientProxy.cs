using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.UnitTests.Fakes;

public sealed class FakeClientProxy(string target, IReadOnlyList<string> excluded, List<SentMessage> sink) : IClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        sink.Add(new SentMessage(target, excluded, method, args.Length > 0 ? args[0] : null));
        return Task.CompletedTask;
    }
}
