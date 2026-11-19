using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class RoomNameParserTests
{
    private static RoomNameParser Parser()
    {
        return new RoomNameParser(new WordLists
        {
            Opinion = ["Adoring", "Lewd"], Size = ["Big"], Shape = ["Round"], Nouns = ["people", "cat"], Unsafe = ["lewd"]
        });
    }

    [Fact]
    public void ParseWords_FunName_ReturnsTheThreeWordsInOrder()
    {
        var words = Parser().ParseWords("42_Adoring_Big_people");

        Assert.NotNull(words);
        Assert.Equal(["Adoring", "Big", "people"], words.Select(w => w.Text));
        Assert.All(words, w => Assert.True(w.Linkable));
    }

    [Fact]
    public void ParseWords_IgnoresCase()
    {
        var words = Parser().ParseWords("7_adoring_BIG_People");

        Assert.NotNull(words);
        Assert.Equal(["adoring", "BIG", "People"], words.Select(w => w.Text));
    }

    [Fact]
    public void ParseWords_UnsafeWord_IsNotLinkable()
    {
        var words = Parser().ParseWords("5_Lewd_Big_cat");

        Assert.NotNull(words);
        Assert.Equal([false, true, true], words.Select(w => w.Linkable));
    }

    [Theory]
    [InlineData("sprint_42_planning")]
    [InlineData("42_Adoring_Big")]
    [InlineData("42_Adoring_Big_people_extra")]
    [InlineData("_Adoring_Big_people")]
    [InlineData("x_Adoring_Big_people")]
    [InlineData("100_Adoring_Big_people")]
    [InlineData("42_Unknown_Big_people")]
    [InlineData("42_Adoring_Big_nothing")]
    [InlineData("42_people_Big_Adoring")]
    [InlineData("")]
    public void ParseWords_NotAFunName_ReturnsNull(string name)
    {
        Assert.Null(Parser().ParseWords(name));
    }
}
