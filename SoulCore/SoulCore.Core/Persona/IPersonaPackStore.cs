namespace SoulCore.Core.Persona;

/// <summary>Persist / load PersonaPack JSON under the CreatorEdition personas root.</summary>
public interface IPersonaPackStore
{
    string RootDirectory { get; }

    Task EnsureSeededAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonaPack>> ListAsync(CancellationToken cancellationToken = default);

    Task<PersonaPack?> GetAsync(string personaId, CancellationToken cancellationToken = default);

    Task<PersonaPack> UpsertAsync(PersonaPack pack, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string personaId, CancellationToken cancellationToken = default);

    Task<string> GetActivePersonaIdAsync(CancellationToken cancellationToken = default);

    Task SetActivePersonaIdAsync(string personaId, CancellationToken cancellationToken = default);
}
