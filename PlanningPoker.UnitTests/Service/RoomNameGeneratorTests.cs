using System.Text.RegularExpressions;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class RoomNameGeneratorTests
{
    private static WordLists Words()
    {
        return new WordLists
        {
            Opinion = ["op"], Size = ["sz"], Shape = ["sh"], Age = ["ag"], Colour = ["co"], Origin = ["or"], Material = ["ma"], Nouns = ["noun"]
        };
    }

    [Fact]
    public void Generate_MissingRequiredLists_FallsBackToGuid()
    {
        var generator = new RoomNameGenerator(new WordLists());

        Assert.True(Guid.TryParse(generator.Generate(), out _));
    }

    [Fact]
    public void Generate_UsesNumberTwoAdjectivesAndNoun()
    {
        var generator = new RoomNameGenerator(Words());

        for (var i = 0; i < 50; i++)
        {
            Assert.Matches(@"^\d{1,2}_[a-z]{2}_[a-z]{2}_noun$", generator.Generate());
        }
    }

    [Fact]
    public void Generate_KeepsAdjectiveCategoriesInFixedOrder()
    {
        var order = new[] { "op", "sz", "sh", "ag", "co", "or", "ma" };
        var generator = new RoomNameGenerator(Words());

        for (var i = 0; i < 50; i++)
        {
            var parts = generator.Generate().Split('_');
            Assert.True(Array.IndexOf(order, parts[1]) < Array.IndexOf(order, parts[2]));
        }
    }

    [Fact]
    public void Generate_EmptyOptionalList_WritesUnknown()
    {
        var words = new WordLists { Opinion = ["op"], Size = ["sz"], Nouns = ["noun"] };
        var generator = new RoomNameGenerator(words);

        var names = Enumerable.Range(0, 100).Select(_ => generator.Generate()).ToList();

        Assert.Contains(names, name => Regex.IsMatch(name, "unknown"));
    }
}
