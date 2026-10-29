using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

/// <summary>A room with two people, both connected and joined. Created by <see cref="TestSession.CreateTestRoomAsync"/>.</summary>
public sealed class TestRoom(string roomId, HubConnection owner, HubConnection guest) : IAsyncDisposable
{
    public string RoomId { get; } = roomId;

    public HubConnection Owner { get; } = owner;

    public HubConnection Guest { get; } = guest;

    public async ValueTask DisposeAsync()
    {
        await Owner.DisposeAsync();
        await Guest.DisposeAsync();
    }
}
