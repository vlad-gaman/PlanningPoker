using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

/// <summary>
/// Keeps each room as one JSON document in a SQLite file. The file is created on first use. A file that cannot be read, or
/// that holds another schema version, is set aside (renamed) and replaced by an empty store, so a storage fault never stops the site.
/// </summary>
public sealed class SqliteRoomStore : IRoomStore
{
    private readonly SqliteConnectionStringBuilder _connection;
    private readonly ILogger<SqliteRoomStore> _logger;
    private readonly object _gate = new();
    private bool _ready;

    public SqliteRoomStore(IOptions<PersistenceOptions> options, ILogger<SqliteRoomStore> logger)
    {
        // Pooling would keep the file open after the last connection closes, which blocks renaming it aside.
        _connection = new SqliteConnectionStringBuilder(options.Value.ConnectionString) { Pooling = false };
        _logger = logger;
    }

    public IReadOnlyList<StoredRoom> LoadAll()
    {
        lock (_gate)
        {
            try
            {
                EnsureReady();
                using var connection = Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT id, json FROM rooms";
                using var reader = command.ExecuteReader();
                var rooms = new List<StoredRoom>();
                while (reader.Read())
                {
                    try
                    {
                        rooms.Add(JsonSerializer.Deserialize<StoredRoom>(reader.GetString(1))!);
                    }
                    catch (JsonException exception)
                    {
                        _logger.LogError(exception, "Skipping stored room {RoomId} that cannot be read", reader.GetString(0));
                    }
                }

                return rooms;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "The room store cannot be read; starting with no rooms");
                return [];
            }
        }
    }

    public void Save(StoredRoom room)
    {
        lock (_gate)
        {
            EnsureReady();
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO rooms (id, json) VALUES ($id, $json) ON CONFLICT(id) DO UPDATE SET json = excluded.json";
            command.Parameters.AddWithValue("$id", room.Room.Id);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(room));
            command.ExecuteNonQuery();
        }
    }

    public void Delete(string roomId)
    {
        lock (_gate)
        {
            EnsureReady();
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM rooms WHERE id = $id";
            command.Parameters.AddWithValue("$id", roomId);
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connection.ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Creates the schema when the file is new and sets the file aside when it is unreadable or of another version. Runs once; callers hold the gate.</summary>
    private void EnsureReady()
    {
        if (_ready)
        {
            return;
        }

        try
        {
            Initialize();
        }
        catch (Exception exception) when (SetAside(exception))
        {
            Initialize();
        }

        _ready = true;
    }

    private void Initialize()
    {
        if (_connection.DataSource is { Length: > 0 } path && path != ":memory:" && Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL); " +
                              "CREATE TABLE IF NOT EXISTS rooms (id TEXT PRIMARY KEY, json TEXT NOT NULL); " +
                              "INSERT OR IGNORE INTO meta (key, value) VALUES ('schema_version', $version); " +
                              "SELECT value FROM meta WHERE key = 'schema_version'";
        command.Parameters.AddWithValue("$version", PersistenceConstants.SchemaVersion.ToString());
        var version = (string?)command.ExecuteScalar();
        if (version != PersistenceConstants.SchemaVersion.ToString())
        {
            throw new InvalidDataException($"The room store has schema version {version}, this server reads {PersistenceConstants.SchemaVersion}.");
        }
    }

    /// <summary>Renames the unusable file so nothing is lost and a fresh one can be created. Returns false when there is no file to set aside.</summary>
    private bool SetAside(Exception exception)
    {
        var path = _connection.DataSource;
        if (string.IsNullOrEmpty(path) || path == ":memory:" || !File.Exists(path))
        {
            return false;
        }

        var aside = $"{path}.unreadable-{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogError(exception, "The room store {Path} cannot be used; moving it to {Aside} and starting with no rooms", path, aside);
        File.Move(path, aside);
        return true;
    }
}
