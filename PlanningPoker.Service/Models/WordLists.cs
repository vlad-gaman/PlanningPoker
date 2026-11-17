namespace PlanningPoker.Service.Models;

public class WordLists
{
    public IReadOnlyList<string> Opinion { get; init; } = [];

    public IReadOnlyList<string> Size { get; init; } = [];

    public IReadOnlyList<string> Shape { get; init; } = [];

    public IReadOnlyList<string> Age { get; init; } = [];

    public IReadOnlyList<string> Colour { get; init; } = [];

    public IReadOnlyList<string> Origin { get; init; } = [];

    public IReadOnlyList<string> Material { get; init; } = [];

    public IReadOnlyList<string> Nouns { get; init; } = [];
}
