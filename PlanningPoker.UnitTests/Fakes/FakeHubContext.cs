using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Controller.Hubs;

namespace PlanningPoker.UnitTests.Fakes;

public sealed class FakeHubContext : IHubContext<RoomHub>
{
    public FakeHubClients Clients { get; } = new();

    public FakeGroupManager Groups { get; } = new();

    IHubClients IHubContext<RoomHub>.Clients => Clients;

    IGroupManager IHubContext<RoomHub>.Groups => Groups;
}
