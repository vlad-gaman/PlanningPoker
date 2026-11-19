using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomNameParser(WordLists words) : IRoomNameParser
{
    private const char Separator = '_';
    private const int PartCount = 4;
    private const int MaxNumberLength = 2;

    private readonly HashSet<string> adjectives = new(
        [.. words.Opinion, .. words.Size, .. words.Shape, .. words.Age, .. words.Colour, .. words.Origin, .. words.Material],
        StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> nouns = new(words.Nouns, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unsafeWords = new(words.Unsafe, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RoomNameWord>? ParseWords(string roomName)
    {
        var parts = roomName.Split(Separator);
        if (parts.Length != PartCount || !IsNumber(parts[0]) || !adjectives.Contains(parts[1]) || !adjectives.Contains(parts[2]) || !nouns.Contains(parts[3]))
        {
            return null;
        }

        return parts.Skip(1).Select(part => new RoomNameWord { Text = part, Linkable = !unsafeWords.Contains(part) }).ToList();
    }

    private static bool IsNumber(string part)
    {
        return part.Length is > 0 and <= MaxNumberLength && part.All(char.IsAsciiDigit);
    }
}
