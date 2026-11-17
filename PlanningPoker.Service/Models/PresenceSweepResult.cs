namespace PlanningPoker.Service.Models;

public class PresenceSweepResult
{
    public static readonly PresenceSweepResult None = new();

    /// <summary>People who became offgrid in this sweep.</summary>
    public IReadOnlyList<PersonOffgridResult> Offgrid { get; init; } = [];

    /// <summary>People removed in this sweep because they stayed offgrid for the whole removal window.</summary>
    public IReadOnlyList<ExitRoomResult> Removed { get; init; } = [];

    /// <summary>Rooms deleted in this sweep because nobody had been online in them for the empty-room lifetime.</summary>
    public IReadOnlyList<string> DisposedRoomIds { get; init; } = [];
}
