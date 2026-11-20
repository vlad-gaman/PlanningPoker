using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class WordListLoaderTests
{
    [Fact]
    public void Load_ReadsEveryEmbeddedListWithCleanWords()
    {
        var lists = new WordListLoader().Load();

        var all = new[] { lists.Opinion, lists.Size, lists.Shape, lists.Age, lists.Colour, lists.Origin, lists.Material, lists.Nouns, lists.Unsafe };
        Assert.All(all, list => Assert.NotEmpty(list));
        Assert.All(all.SelectMany(list => list), word =>
        {
            Assert.DoesNotContain(',', word);
            Assert.DoesNotContain('\uFEFF', word);
            Assert.Equal(word.Trim(), word);
        });
        Assert.Contains("people", lists.Nouns);
    }

    [Fact]
    public void GeneratedNames_AreRecognisedByTheParser()
    {
        var lists = new WordListLoader().Load();
        var generator = new RoomNameGenerator(lists);
        var parser = new RoomNameParser(lists);

        for (var i = 0; i < 500; i++)
        {
            var name = generator.Generate();
            Assert.NotNull(parser.ParseWords(name));
        }
    }

    [Fact]
    public void UnsafeWords_AreOnlyWordsFromTheOtherLists()
    {
        var lists = new WordListLoader().Load();
        var known = new[] { lists.Opinion, lists.Size, lists.Shape, lists.Age, lists.Colour, lists.Origin, lists.Material, lists.Nouns }
            .SelectMany(list => list).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(lists.Unsafe, word => Assert.Contains(word, known));
    }
}
