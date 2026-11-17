using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>How the countdown ticker reaches the room clients; the Controller layer implements it so the Service layer never references SignalR.</summary>
public interface IRoomCountdownNotifier
{
    Task CountdownTickedAsync(string roomId, int remaining);

    Task VotesRevealedAsync(string roomId, VoteStatus status);
}
