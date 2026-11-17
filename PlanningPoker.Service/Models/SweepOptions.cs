using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class SweepOptions
{
    /// <summary>A room or person untouched for longer than this is abandoned.</summary>
    public TimeSpan AbandonedAfter { get; set; } = SweepConstants.AbandonedAfter;

    /// <summary>How often the background sweep runs.</summary>
    public TimeSpan Interval { get; set; } = SweepConstants.Interval;
}
