namespace SoulCore.Core.Persona;

/// <summary>
/// Active-persona session. v1 loads exactly one active pack; the API is framed
/// so a future multi-active map can be additive (keyed by <c>personaId</c>).
/// </summary>
/// <remarks>
/// Live trait / pack edits apply on the <b>next</b> chat turn — <see cref="GetActive"/>
/// is read at context-build time, not mid-inference.
/// </remarks>
public interface IPersonaSession
{
    /// <summary>Currently active persona id (single-active v1).</summary>
    string ActivePersonaId { get; }

    /// <summary>Clone of the active pack (safe to mutate without touching session).</summary>
    PersonaPack GetActive();

    /// <summary>
    /// Framed multi-active lookup: v1 returns true only for the active id.
    /// Future: warm packs may appear here without rewriting callers.
    /// </summary>
    bool TryGet(string personaId, out PersonaPack pack);

    /// <summary>Snapshot of packs currently considered loaded (v1: active only).</summary>
    IReadOnlyDictionary<string, PersonaPack> GetLoadedSnapshot();

    Task SetActiveAsync(string personaId, CancellationToken cancellationToken = default);

    /// <summary>Reload active pack from store (after upsert of the active id).</summary>
    Task ReloadActiveAsync(CancellationToken cancellationToken = default);
}
