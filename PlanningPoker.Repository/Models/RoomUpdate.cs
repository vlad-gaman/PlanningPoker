namespace PlanningPoker.Repository.Models;

/// <summary>What a room mutation returns: its result, and whether it changed the room's state.</summary>
public sealed record RoomUpdate<T>(T Result, bool Changed);

/// <summary>Builds <see cref="RoomUpdate{T}"/> values so the caller does not repeat the type argument.</summary>
public static class RoomUpdate
{
    public static RoomUpdate<T> Changed<T>(T result) => new(result, true);

    public static RoomUpdate<T> Unchanged<T>(T result) => new(result, false);

    public static RoomUpdate<T> Of<T>(T result, bool changed) => new(result, changed);
}
