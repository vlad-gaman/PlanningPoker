namespace PlanningPoker.Service.Models;

/// <summary>What one presence pass changed in one room, decided under the room lock and read out after it.</summary>
/// <param name="Offgrid">Members whose grace period lapsed in this pass.</param>
/// <param name="Removed">Members whose removal window passed in this pass.</param>
/// <param name="PreviousOwnerGuid">The owner who went offgrid or was removed, when the room had to hand over.</param>
/// <param name="NewOwnerGuid">Who the room was handed to.</param>
/// <param name="Disposed">True when nobody had been online long enough and the room was disposed.</param>
internal sealed record PresenceChange(List<Guid> Offgrid, List<Guid> Removed, Guid? PreviousOwnerGuid, Guid? NewOwnerGuid, bool Disposed)
{
    public bool IsEmpty => !Disposed && Offgrid.Count == 0 && Removed.Count == 0;
}
