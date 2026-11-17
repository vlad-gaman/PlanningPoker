namespace PlanningPoker.Service.Services;

public interface IRoomRestorer
{
    /// <summary>Loads the stored rooms into memory, every person of them offgrid until they reconnect. Returns how many rooms came back.</summary>
    int Restore();
}
