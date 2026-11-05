using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoulCore.Config;
using SoulCore.Core.SharedSpaces;

namespace SoulCore.Host.SharedSpaces;

public sealed class SharedSpaceStore : ISharedSpaceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly SharedSpaceOptions _options;
    private readonly ILogger<SharedSpaceStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SharedSpaceStore(IOptions<SharedSpaceOptions> options, ILogger<SharedSpaceStore> logger)
    {
        _options = options?.Value ?? new SharedSpaceOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RootDirectory = _options.ResolveRootDirectory();
    }

    public SharedSpaceStore(string rootDirectory, ILogger<SharedSpaceStore>? logger = null)
    {
        _options = new SharedSpaceOptions { RootDirectory = rootDirectory };
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SharedSpaceStore>.Instance;
        RootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string RootDirectory { get; }

    public async Task<IReadOnlyList<SharedSpace>> ListAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(RootDirectory);
            var list = new List<SharedSpace>();
            foreach (var dir in Directory.EnumerateDirectories(RootDirectory))
            {
                var meta = Path.Combine(dir, SharedSpacePaths.MetaFileName);
                if (!File.Exists(meta))
                    continue;
                var space = await ReadMetaAsync(meta, ct).ConfigureAwait(false);
                if (space is not null)
                    list.Add(space);
            }

            list.Sort((a, b) => string.Compare(a.SpaceId, b.SpaceId, StringComparison.OrdinalIgnoreCase));
            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SharedSpace?> GetAsync(string spaceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        var id = SharedSpacePaths.NormalizeSpaceId(spaceId);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var path = SharedSpacePaths.ResolveMetaPath(RootDirectory, id);
            if (!File.Exists(path))
                return null;
            return await ReadMetaAsync(path, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SharedSpace> UpsertAsync(SharedSpace space, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(space);
        ArgumentException.ThrowIfNullOrWhiteSpace(space.SpaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(space.DisplayName);

        var id = SharedSpacePaths.NormalizeSpaceId(space.SpaceId);
        var name = space.DisplayName.Trim();
        if (name.Length > SharedSpace.NameMaxLength)
            throw new ArgumentException($"DisplayName max length is {SharedSpace.NameMaxLength}.");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(SharedSpacePaths.ResolveSpaceDirectory(RootDirectory, id));
            var path = SharedSpacePaths.ResolveMetaPath(RootDirectory, id);
            SharedSpace existing = new();
            if (File.Exists(path))
            {
                existing = await ReadMetaAsync(path, ct).ConfigureAwait(false) ?? new SharedSpace();
            }

            var next = new SharedSpace
            {
                SpaceId = id,
                DisplayName = name,
                Description = space.Description,
                CreatedAtUtc = existing.CreatedAtUtc == default ? DateTimeOffset.UtcNow : existing.CreatedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(next, JsonOptions);
            await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);

            // Touch DB so schema exists.
            using (OpenMemoryUnlocked(id))
            {
            }

            _logger.LogInformation("Shared space upserted {SpaceId}", id);
            return next;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(string spaceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        var id = SharedSpacePaths.NormalizeSpaceId(spaceId);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var dir = SharedSpacePaths.ResolveSpaceDirectory(RootDirectory, id);
            if (!Directory.Exists(dir))
                return false;
            Directory.Delete(dir, recursive: true);
            _logger.LogInformation("Shared space deleted {SpaceId}", id);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public SharedSpaceMemory OpenMemory(string spaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        var id = SharedSpacePaths.NormalizeSpaceId(spaceId);
        return OpenMemoryUnlocked(id);
    }

    private SharedSpaceMemory OpenMemoryUnlocked(string spaceId) =>
        new(SharedSpacePaths.ResolveDbPath(RootDirectory, spaceId));

    private static async Task<SharedSpace?> ReadMetaAsync(string path, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var space = JsonSerializer.Deserialize<SharedSpace>(json, JsonOptions);
        if (space is null || string.IsNullOrWhiteSpace(space.SpaceId))
            return null;
        return space;
    }
}
