namespace PlanningPoker.Service.Services;

public interface ILoginLockoutService
{
    /// <summary>True when sign-in is refused for this address, or for the Operator account.</summary>
    bool IsLocked(string? ipAddress);

    void RecordFailure(string? ipAddress);

    void RecordSuccess(string? ipAddress);
}
