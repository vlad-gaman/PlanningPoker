using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records every broadcast in <see cref="Sent"/> with the target and excluded connections it was addressed to.</summary>
public sealed class FakeHubClients : IHubClients
{
    private static readonly IReadOnlyList<string> NoExclusions = [];

    public List<SentMessage> Sent { get; } = [];

    public IClientProxy All => Proxy("all", NoExclusions);

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy("all", excludedConnectionIds);

    public IClientProxy Client(string connectionId) => Proxy($"client:{connectionId}", NoExclusions);

    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy($"clients:{string.Join(',', connectionIds)}", NoExclusions);

    public IClientProxy Group(string groupName) => Proxy($"group:{groupName}", NoExclusions);

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy($"group:{groupName}", excludedConnectionIds);

    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy($"groups:{string.Join(',', groupNames)}", NoExclusions);

    public IClientProxy User(string userId) => Proxy($"user:{userId}", NoExclusions);

    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy($"users:{string.Join(',', userIds)}", NoExclusions);

    private IClientProxy Proxy(string target, IReadOnlyList<string> excluded)
    {
        return new FakeClientProxy(target, excluded, Sent);
    }
}
