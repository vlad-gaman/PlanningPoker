namespace PlanningPoker.Service.Models;

/// <summary>Why a person's membership ended; it decides whether their vote is kept aside as a Departed vote.</summary>
public enum LeaveReason
{
    /// <summary>The person left on purpose: their vote is kept as a Departed vote.</summary>
    Deliberate,

    /// <summary>The removal window passed while they were offgrid: their vote is discarded.</summary>
    Timeout,

    /// <summary>An operator removed them: their vote is discarded.</summary>
    OperatorRemoval
}
