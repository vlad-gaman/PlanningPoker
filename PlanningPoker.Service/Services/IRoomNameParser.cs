using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IRoomNameParser
{
    /// <summary>Splits a fun room name into its three words (two adjectives and a noun); null when the name is not a fun room name.</summary>
    IReadOnlyList<RoomNameWord>? ParseWords(string roomName);
}
