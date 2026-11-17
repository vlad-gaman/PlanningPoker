using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

/// <summary>Keeps the issues log in an <c>issues</c> table of the same SQLite file as the rooms. The table is created on first use.</summary>
public sealed class SqliteIssueStore : IIssueStore
{
    private readonly SqliteConnectionStringBuilder _connection;
    private readonly object _gate = new();

    public SqliteIssueStore(IOptions<PersistenceOptions> options)
    {
        // Same as the room store: no pooling, so the file is never held open after the last connection closes.
        _connection = new SqliteConnectionStringBuilder(options.Value.ConnectionString) { Pooling = false };
    }

    public void Add(IssueEntry entry)
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO issues (time, level, source, message, ip) VALUES ($time, $level, $source, $message, $ip)";
            command.Parameters.AddWithValue("$time", entry.Time.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$level", (int)entry.Level);
            command.Parameters.AddWithValue("$source", entry.Source);
            command.Parameters.AddWithValue("$message", entry.Message);
            command.Parameters.AddWithValue("$ip", (object?)entry.IpAddress ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<IssueEntry> Recent(int limit)
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, time, level, source, message, ip FROM issues ORDER BY id DESC LIMIT $limit";
            command.Parameters.AddWithValue("$limit", limit);
            using var reader = command.ExecuteReader();
            var entries = new List<IssueEntry>();
            while (reader.Read())
            {
                entries.Add(new IssueEntry
                {
                    Id = reader.GetInt64(0),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)),
                    Level = (IssueLevel)reader.GetInt32(2),
                    Source = reader.GetString(3),
                    Message = reader.GetString(4),
                    IpAddress = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return entries;
        }
    }

    public int CountFailedLogins(DateTimeOffset since)
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM issues WHERE ip IS NOT NULL AND time >= $since";
            command.Parameters.AddWithValue("$since", since.ToUnixTimeMilliseconds());
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public void Prune(DateTimeOffset olderThan, int maxEntries)
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM issues WHERE time < $cutoff; " +
                                  "DELETE FROM issues WHERE id NOT IN (SELECT id FROM issues ORDER BY id DESC LIMIT $max)";
            command.Parameters.AddWithValue("$cutoff", olderThan.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$max", maxEntries);
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection Open()
    {
        if (_connection.DataSource is { Length: > 0 } path && path != ":memory:" && Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection(_connection.ConnectionString);
        try
        {
            connection.Open();
            using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE IF NOT EXISTS issues (id INTEGER PRIMARY KEY AUTOINCREMENT, time INTEGER NOT NULL, level INTEGER NOT NULL, " +
                                 "source TEXT NOT NULL, message TEXT NOT NULL, ip TEXT NULL)";
            create.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            // An open handle on a damaged file would stop the room store from setting it aside.
            connection.Dispose();
            throw;
        }
    }
}
