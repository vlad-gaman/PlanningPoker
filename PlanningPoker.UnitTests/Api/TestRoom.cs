using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

/// <summary>A room with two people, both connected and joined. Created by <see cref="TestSession.CreateTestRoomAsync"/>.</summary>
public sealed class TestRoom(string roomId, HubConnection owner, HubConnection guest, string ownerCookie) : IAsyncDisposable
{
    public string RoomId { get; } = roomId;

    /// <summary>The owner's session cookie, for opening a second connection as the same person.</summary>
    public string OwnerCookie { get; } = ownerCookie;

    public HubConnection Owner { get; } = owner;

    public HubConnection Guest { get; } = guest;

    public async ValueTask DisposeAsync()
    {
        await Owner.DisposeAsync();
        await Guest.DisposeAsync();
    }
}
