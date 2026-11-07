using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoulCore.Config;
using SoulCore.Core.Wellbeing;

namespace SoulCore.Host.Wellbeing;

public sealed class ObservatoryStore : IObservatoryStore, IAsyncDisposable
{
    private readonly string _dbPath;
    private readonly ILogger<ObservatoryStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SqliteConnection? _conn;

    public ObservatoryStore(IOptions<PersonaOptions> options, ILogger<ObservatoryStore> logger)
    {
        var root = (options?.Value ?? new PersonaOptions()).ResolveRootDirectory();
        _dbPath = ObservatoryPaths.ResolveDatabasePath(root);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Test helper.</summary>
    public ObservatoryStore(string databasePath, ILogger<ObservatoryStore>? logger = null)
    {
        _dbPath = Path.GetFullPath(databasePath);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ObservatoryStore>.Instance;
    }

    public async Task<StimulusRecord> RecordStimulusAsync(
        string kind,
        string? personaId,
        string? relatedPersonaId,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        var id = Guid.NewGuid().ToString("N");
        var at = DateTimeOffset.UtcNow.ToString("o");
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = _conn!.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO stimuli (stimulus_id, kind, persona_id, related_persona_id, at_utc, payload_json)
                VALUES ($id, $kind, $pid, $rid, $at, $payload);
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$kind", kind.Trim());
            cmd.Parameters.AddWithValue("$pid", (object?)personaId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rid", (object?)relatedPersonaId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$at", at);
            cmd.Parameters.AddWithValue("$payload", (object?)payloadJson ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        _logger.LogInformation("Observatory stimulus {Kind} id={Id} persona={PersonaId}", kind, id, personaId);
        return new StimulusRecord(id, kind.Trim(), personaId, relatedPersonaId, at, payloadJson, null, null);
    }

    public async Task AttachResponseAsync(
        string stimulusId,
        string channel,
        string? personaId,
        string body,
        string? metaJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stimulusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        var at = DateTimeOffset.UtcNow.ToString("o");
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = _conn!.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO responses (stimulus_id, channel, persona_id, body, meta_json, at_utc)
                VALUES ($sid, $ch, $pid, $body, $meta, $at);
                """;
            cmd.Parameters.AddWithValue("$sid", stimulusId.Trim());
            cmd.Parameters.AddWithValue("$ch", channel.Trim());
            cmd.Parameters.AddWithValue("$pid", (object?)personaId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$body", body);
            cmd.Parameters.AddWithValue("$meta", (object?)metaJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$at", at);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AnnotateAsync(
        string stimulusId,
        string note,
        string? phenomenonId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stimulusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(note);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = _conn!.CreateCommand();
            cmd.CommandText =
                """
                UPDATE stimuli
                SET operator_note = $note,
                    phenomenon_id = COALESCE($phen, phenomenon_id)
                WHERE stimulus_id = $id;
                """;
            cmd.Parameters.AddWithValue("$note", note.Trim());
            cmd.Parameters.AddWithValue("$phen", (object?)phenomenonId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", stimulusId.Trim());
            var n = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (n == 0)
                throw new KeyNotFoundException($"Unknown stimulus_id '{stimulusId}'.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<StimulusRecord>> ListStimuliAsync(
        int limit = 50,
        string? kind = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = _conn!.CreateCommand();
            if (string.IsNullOrWhiteSpace(kind))
            {
                cmd.CommandText =
                    """
                    SELECT stimulus_id, kind, persona_id, related_persona_id, at_utc, payload_json, operator_note, phenomenon_id
                    FROM stimuli
                    ORDER BY at_utc DESC
                    LIMIT $lim;
                    """;
            }
            else
            {
                cmd.CommandText =
                    """
                    SELECT stimulus_id, kind, persona_id, related_persona_id, at_utc, payload_json, operator_note, phenomenon_id
                    FROM stimuli
                    WHERE kind = $kind
                    ORDER BY at_utc DESC
                    LIMIT $lim;
                    """;
                cmd.Parameters.AddWithValue("$kind", kind.Trim());
            }

            cmd.Parameters.AddWithValue("$lim", limit);
            var list = new List<StimulusRecord>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                list.Add(ReadStimulus(reader));
            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<StimulusDetail?> GetAsync(string stimulusId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stimulusId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            StimulusRecord? stim = null;
            await using (var cmd = _conn!.CreateCommand())
            {
                cmd.CommandText =
                    """
                    SELECT stimulus_id, kind, persona_id, related_persona_id, at_utc, payload_json, operator_note, phenomenon_id
                    FROM stimuli WHERE stimulus_id = $id LIMIT 1;
                    """;
                cmd.Parameters.AddWithValue("$id", stimulusId.Trim());
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    stim = ReadStimulus(reader);
            }

            if (stim is null)
                return null;

            var responses = new List<ResponseSnippet>();
            await using (var cmd = _conn!.CreateCommand())
            {
                cmd.CommandText =
                    """
                    SELECT id, stimulus_id, channel, persona_id, body, meta_json, at_utc
                    FROM responses WHERE stimulus_id = $id ORDER BY id ASC;
                    """;
                cmd.Parameters.AddWithValue("$id", stimulusId.Trim());
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    responses.Add(new ResponseSnippet(
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3),
                        reader.GetString(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.GetString(6)));
                }
            }

            return new StimulusDetail(stim, responses);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string?> TryGetOpenSoftSwitchStimulusIdAsync(
        string personaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personaId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = _conn!.CreateCommand();
            cmd.CommandText =
                """
                SELECT stimulus_id FROM stimuli
                WHERE kind = $kind AND persona_id = $pid
                ORDER BY at_utc DESC
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$kind", StimulusKinds.PersonaSoftSwitch);
            cmd.Parameters.AddWithValue("$pid", personaId.Trim());
            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result as string;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static StimulusRecord ReadStimulus(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7));

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (_conn is not null)
            return;

        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _conn = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        await _conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS stimuli (
              stimulus_id TEXT PRIMARY KEY,
              kind TEXT NOT NULL,
              persona_id TEXT NULL,
              related_persona_id TEXT NULL,
              at_utc TEXT NOT NULL,
              payload_json TEXT NULL,
              operator_note TEXT NULL,
              phenomenon_id TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_stimuli_kind_at ON stimuli (kind, at_utc DESC);
            CREATE TABLE IF NOT EXISTS responses (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              stimulus_id TEXT NOT NULL REFERENCES stimuli(stimulus_id) ON DELETE CASCADE,
              channel TEXT NOT NULL,
              persona_id TEXT NULL,
              body TEXT NOT NULL,
              meta_json TEXT NULL,
              at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_responses_stimulus ON responses (stimulus_id, id);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_conn is not null)
            {
                await _conn.DisposeAsync().ConfigureAwait(false);
                _conn = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
