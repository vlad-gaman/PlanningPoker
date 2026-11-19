namespace PlanningPoker.Controller.Models;

public class RoomNameWordResponse
{
    public string Text { get; init; } = default!;

    /// <summary>False for a word that must be shown without a search link.</summary>
    public bool Linkable { get; init; }
}
