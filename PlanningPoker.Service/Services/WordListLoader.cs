using System.Reflection;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Reads the word lists embedded in this assembly (one word per line; commas also separate words).</summary>
public class WordListLoader : IWordListLoader
{
    public WordLists Load()
    {
        return new WordLists
        {
            Opinion = Read("OpinionAdjectives"),
            Size = Read("SizeAdjectives"),
            Shape = Read("ShapeAdjectives"),
            Age = Read("AgeAdjectives"),
            Colour = Read("ColourAdjectives"),
            Origin = Read("OriginAdjectives"),
            Material = Read("MaterialAdjectives"),
            Nouns = Read("Nouns")
        };
    }

    private static List<string> Read(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith($".Csvs.{name}.csv", StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        var words = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            words.AddRange(line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        return words;
    }
}
