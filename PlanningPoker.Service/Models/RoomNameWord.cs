namespace PlanningPoker.Service.Models;

/// <summary>One word of a fun room name and whether it may be offered as a search link.</summary>
public class RoomNameWord
{
    public string Text { get; init; } = default!;

    public bool Linkable { get; init; }
}
