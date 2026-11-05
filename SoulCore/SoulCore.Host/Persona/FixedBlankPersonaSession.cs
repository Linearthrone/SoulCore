using SoulCore.Core.Persona;

namespace SoulCore.Host.Persona;

/// <summary>In-memory Blank session for tests / fallback when DI has not initialized packs.</summary>
public sealed class FixedBlankPersonaSession : IPersonaSession
{
    private PersonaPack _active = PersonaPack.CreateBlank();

    public string ActivePersonaId => _active.PersonaId;

    public PersonaPack GetActive() => _active.Clone();

    public bool TryGet(string personaId, out PersonaPack pack)
    {
        if (string.Equals(personaId, _active.PersonaId, StringComparison.OrdinalIgnoreCase))
        {
            pack = _active.Clone();
            return true;
        }

        pack = null!;
        return false;
    }

    public IReadOnlyDictionary<string, PersonaPack> GetLoadedSnapshot() =>
        new Dictionary<string, PersonaPack>(StringComparer.OrdinalIgnoreCase)
        {
            [_active.PersonaId] = _active.Clone()
        };

    public Task SetActiveAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(personaId, PersonaPack.BlankPersonaId, StringComparison.OrdinalIgnoreCase))
            throw new KeyNotFoundException($"FixedBlankPersonaSession only supports '{PersonaPack.BlankPersonaId}'.");
        return Task.CompletedTask;
    }

    public Task ReloadActiveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>Test helper — swap the in-memory pack (next turn).</summary>
    public void ReplaceActive(PersonaPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        _active = pack.Clone();
    }
}
