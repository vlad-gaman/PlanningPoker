using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Repository;

public sealed class SqliteIssueStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), "pp-issues-" + Guid.NewGuid() + ".db");

    private IOptions<PersistenceOptions> Options => Microsoft.Extensions.Options.Options.Create(new PersistenceOptions { ConnectionString = $"Data Source={_path}" });

    private SqliteIssueStore Create() => new(Options);

    private static IssueEntry Entry(string message, DateTimeOffset? time = null, string? ip = null, IssueLevel level = IssueLevel.Warning)
    {
        return new IssueEntry { Time = time ?? Now, Level = level, Source = "Test", Message = message, IpAddress = ip };
    }

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Recent_ReturnsTheNewestFirstWithAllFields()
    {
        var store = Create();
        store.Add(Entry("old", level: IssueLevel.Info));
        store.Add(Entry("new", Now.AddMinutes(1), "203.0.113.7", IssueLevel.Error));

        var entries = store.Recent(10);

        Assert.Equal(["new", "old"], entries.Select(entry => entry.Message));
        Assert.Equal(IssueLevel.Error, entries[0].Level);
        Assert.Equal("203.0.113.7", entries[0].IpAddress);
        Assert.Equal(Now.AddMinutes(1), entries[0].Time);
        Assert.Equal("Test", entries[0].Source);
        Assert.Null(entries[1].IpAddress);
        Assert.True(entries[0].Id > entries[1].Id);
    }

    [Fact]
    public void Recent_HonoursTheLimit()
    {
        var store = Create();
        for (var i = 0; i < 5; i++)
        {
            store.Add(Entry($"entry {i}"));
        }

        Assert.Equal(["entry 4", "entry 3"], store.Recent(2).Select(entry => entry.Message));
    }

    [Fact]
    public void Prune_RemovesEntriesOlderThanTheCutoff()
    {
        var store = Create();
        store.Add(Entry("ancient", Now.AddDays(-10)));
        store.Add(Entry("fresh", Now));

        store.Prune(Now.AddDays(-7), 500);

        Assert.Equal(["fresh"], store.Recent(10).Select(entry => entry.Message));
    }

    [Fact]
    public void Prune_KeepsOnlyTheNewestEntriesUpToTheMaximum()
    {
        var store = Create();
        for (var i = 1; i <= 6; i++)
        {
            store.Add(Entry($"entry {i}"));
        }

        store.Prune(Now.AddDays(-7), 4);

        Assert.Equal(["entry 6", "entry 5", "entry 4", "entry 3"], store.Recent(10).Select(entry => entry.Message));
    }

    [Fact]
    public void ASecondStoreOnTheSameFile_SeesTheEntries()
    {
        Create().Add(Entry("kept across instances"));

        Assert.Equal("kept across instances", Create().Recent(10).Single().Message);
    }

    [Fact]
    public void SharesTheFileWithTheRoomStore()
    {
        var issues = Create();
        var rooms = new SqliteRoomStore(Options, NullLogger<SqliteRoomStore>.Instance);

        issues.Add(Entry("before rooms"));
        Assert.Empty(rooms.LoadAll());
        issues.Add(Entry("after rooms"));

        Assert.Equal(2, issues.Recent(10).Count);
    }
}
