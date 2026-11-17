using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomNameGenerator(WordLists words) : IRoomNameGenerator
{
    private const int AdjectiveCount = 2;

    public string Generate()
    {
        if (words.Nouns.Count == 0 || words.Opinion.Count == 0 || words.Size.Count == 0)
        {
            return Guid.NewGuid().ToString();
        }

        // Adjective categories are always written in the same order (opinion, size, shape, ...), whichever are picked.
        IReadOnlyList<IReadOnlyList<string>> categories = [words.Opinion, words.Size, words.Shape, words.Age, words.Colour, words.Origin, words.Material];
        var picked = Enumerable.Range(0, categories.Count).OrderBy(_ => Random.Shared.Next()).Take(AdjectiveCount).Order();

        var parts = new List<string> { Random.Shared.Next(100).ToString() };
        parts.AddRange(picked.Select(index => Pick(categories[index])));
        parts.Add(Pick(words.Nouns));
        return string.Join('_', parts);
    }

    private static string Pick(IReadOnlyList<string> list)
    {
        return list.Count == 0 ? "unknown" : list[Random.Shared.Next(list.Count)];
    }
}
