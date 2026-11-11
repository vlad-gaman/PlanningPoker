using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Repository;

public sealed class SqliteRoomStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "pp-store-test-" + Guid.NewGuid() + ".db");

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            File.Delete(file);
        }
    }

    private SqliteRoomStore NewStore()
    {
        return new SqliteRoomStore(Options.Create(new PersistenceOptions { ConnectionString = $"Data Source={_path}" }), NullLogger<SqliteRoomStore>.Instance);
    }

    private string[] SetAsideFiles() => Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + ".unreadable-*");

    private static StoredRoom NewRoom(string id, string vote = "5")
    {
        var owner = Guid.NewGuid();
        return new StoredRoom
        {
            SavedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            Room = new RoomEntity { Id = id, OwnerGuid = owner, MemberGuids = [owner], Votes = new Dictionary<Guid, string> { [owner] = vote }, VotingEnabled = true },
            People = [new PersonEntity { Guid = owner, Name = "Ann", PersonType = "dev" }]
        };
    }

    [Fact]
    public void LoadAll_NewFile_IsEmptyAndCreatesTheStore()
    {
        Assert.Empty(NewStore().LoadAll());
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void Save_ThenLoadAll_ReturnsTheRoomWithItsPeopleAndVotes()
    {
        var room = NewRoom("room-a");
        NewStore().Save(room);

        var loaded = Assert.Single(NewStore().LoadAll());

        Assert.Equal("room-a", loaded.Room.Id);
        Assert.Equal("5", loaded.Room.Votes[room.Room.OwnerGuid]);
        Assert.Equal("Ann", Assert.Single(loaded.People).Name);
        Assert.Equal(room.SavedAt, loaded.SavedAt);
    }

    [Fact]
    public void Save_SameRoomTwice_ReplacesIt()
    {
        var store = NewStore();
        store.Save(NewRoom("room-a", "3"));
        store.Save(NewRoom("room-a", "8"));

        var loaded = Assert.Single(store.LoadAll());

        Assert.Equal("8", loaded.Room.Votes.Values.Single());
    }

    [Fact]
    public void Delete_RemovesTheRoom()
    {
        var store = NewStore();
        store.Save(NewRoom("room-a"));
        store.Save(NewRoom("room-b"));

        store.Delete("room-a");

        Assert.Equal("room-b", Assert.Single(store.LoadAll()).Room.Id);
    }

    [Fact]
    public void LoadAll_UnreadableFile_ReturnsNoRoomsSetsTheFileAsideAndKeepsWorking()
    {
        File.WriteAllText(_path, "this is not a sqlite database");
        var store = NewStore();

        Assert.Empty(store.LoadAll());
        store.Save(NewRoom("room-a"));

        Assert.Equal("room-a", Assert.Single(store.LoadAll()).Room.Id);
        Assert.Single(SetAsideFiles());
    }

    [Fact]
    public void LoadAll_StoreOfAnotherSchemaVersion_StartsEmptyAndSetsTheFileAside()
    {
        NewStore().Save(NewRoom("room-a"));
        using (var connection = new SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE meta SET value = '999'";
            command.ExecuteNonQuery();
        }

        Assert.Empty(NewStore().LoadAll());
        Assert.Single(SetAsideFiles());
    }
}
