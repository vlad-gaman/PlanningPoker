using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Returns scripted results for Disconnect and SweepPresence and records the calls made to them.</summary>
public sealed class FakePresenceService : IPresenceService
{
    public ServiceResponse DisconnectResult { get; set; } = ServiceResponse.Success();

    public List<(Guid PersonGuid, string ConnectionId)> DisconnectCalls { get; } = [];

    public ServiceResponse Disconnect(Guid personGuid, string connectionId)
    {
        DisconnectCalls.Add((personGuid, connectionId));
        return DisconnectResult;
    }

    public PresenceSweepResult PresenceSweepResult { get; set; } = PresenceSweepResult.None;

    public int PresenceSweeps { get; private set; }

    /// <summary>Thrown by <see cref="SweepPresence"/> when set.</summary>
    public Exception? SweepPresenceException { get; set; }

    public PresenceSweepResult SweepPresence()
    {
        PresenceSweeps++;
        return SweepPresenceException is { } exception ? throw exception : PresenceSweepResult;
    }
}
