using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class WordListLoaderTests
{
    [Fact]
    public void Load_ReadsEveryEmbeddedListWithCleanWords()
    {
        var lists = new WordListLoader().Load();

        var all = new[] { lists.Opinion, lists.Size, lists.Shape, lists.Age, lists.Colour, lists.Origin, lists.Material, lists.Nouns };
        Assert.All(all, list => Assert.NotEmpty(list));
        Assert.All(all.SelectMany(list => list), word =>
        {
            Assert.DoesNotContain(',', word);
            Assert.DoesNotContain('\uFEFF', word);
            Assert.Equal(word.Trim(), word);
        });
        Assert.Contains("people", lists.Nouns);
    }
}
