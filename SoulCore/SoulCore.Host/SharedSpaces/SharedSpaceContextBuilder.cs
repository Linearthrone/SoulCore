using SoulCore.Core.Persona;

namespace SoulCore.Host.SharedSpaces;

/// <summary>
/// Builds prompt lines from shared spaces attached to the active pack (PROP-20.2).
/// Private persona memory is never read here.
/// </summary>
public interface ISharedSpaceContextBuilder
{
    Task<string> BuildAttachedContextAsync(PersonaPack pack, int perSpaceLimit = 8, CancellationToken ct = default);
}

public sealed class SharedSpaceContextBuilder : ISharedSpaceContextBuilder
{
    private readonly ISharedSpaceStore _store;

    public SharedSpaceContextBuilder(ISharedSpaceStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public async Task<string> BuildAttachedContextAsync(
        PersonaPack pack,
        int perSpaceLimit = 8,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var ids = pack.SharedSpaceIds ?? [];
        if (ids.Length == 0)
            return string.Empty;

        var blocks = new List<string>();
        foreach (var rawId in ids)
        {
            if (string.IsNullOrWhiteSpace(rawId))
                continue;
            var space = await _store.GetAsync(rawId, ct).ConfigureAwait(false);
            if (space is null)
                continue;

            using var mem = _store.OpenMemory(space.SpaceId);
            var facts = await mem.RecallRecentAsync(perSpaceLimit, ct).ConfigureAwait(false);
            if (facts.Count == 0)
            {
                blocks.Add($"[SharedSpace:{space.SpaceId} {space.DisplayName}] (empty)");
                continue;
            }

            blocks.Add(
                $"[SharedSpace:{space.SpaceId} {space.DisplayName}]\n"
                + string.Join("\n", facts.Select(f => "- " + f)));
        }

        return blocks.Count == 0 ? string.Empty : string.Join("\n\n", blocks);
    }
}
