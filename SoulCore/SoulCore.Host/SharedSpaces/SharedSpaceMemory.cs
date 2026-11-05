using Microsoft.Data.Sqlite;

namespace SoulCore.Host.SharedSpaces;

/// <summary>
/// Minimal episodic store for a shared space SQLite file (PROP-20.2).
/// Separate from persona private DBs — no charter/journals here.
/// </summary>
public sealed class SharedSpaceMemory : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public SharedSpaceMemory(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        DatabasePath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        _connection.Open();
        EnsureSchema();
    }

    public string DatabasePath { get; }

    public async Task<long> WriteFactAsync(string text, string sourceLabel, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO shared_facts (content, source_label, created_at)
                VALUES ($c, $s, $t);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$c", text.Trim());
            cmd.Parameters.AddWithValue("$s", string.IsNullOrWhiteSpace(sourceLabel) ? "operator" : sourceLabel.Trim());
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToString("O"));
            var id = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt64(id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<string>> RecallRecentAsync(int limit, CancellationToken ct = default)
    {
        if (limit < 1)
            limit = 1;
        if (limit > 50)
            limit = 50;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT content FROM shared_facts
                ORDER BY id DESC
                LIMIT $n;
                """;
            cmd.Parameters.AddWithValue("$n", limit);
            var list = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                list.Add(reader.GetString(0));
            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS shared_facts (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              content TEXT NOT NULL,
              source_label TEXT NOT NULL,
              created_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SharedSpaceMemory));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _gate.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        _gate.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
