using SoulCore.Core.Persona;

namespace SoulCore.Host.Persona;

/// <summary>Test/fallback session that always exposes Blank.</summary>
public sealed class NullPersonaSession : IPersonaSession
{
    private readonly PersonaPack _pack = PersonaPack.CreateBlank();

    public string ActivePersonaId => _pack.PersonaId;

    public PersonaPack GetActive() => _pack.Clone();

    public bool TryGet(string personaId, out PersonaPack pack)
    {
        if (string.Equals(personaId, _pack.PersonaId, StringComparison.OrdinalIgnoreCase))
        {
            pack = _pack.Clone();
            return true;
        }

        pack = null!;
        return false;
    }

    public IReadOnlyDictionary<string, PersonaPack> GetLoadedSnapshot() =>
        new Dictionary<string, PersonaPack>(StringComparer.OrdinalIgnoreCase)
        {
            [_pack.PersonaId] = _pack.Clone()
        };

    public Task SetActiveAsync(string personaId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ReloadActiveAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
