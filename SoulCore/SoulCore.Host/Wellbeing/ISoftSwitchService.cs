using SoulCore.Core.Persona;

namespace SoulCore.Host.Wellbeing;

/// <summary>PROP-21.1 — soft persona handoff (hard cutover forbidden).</summary>
public interface ISoftSwitchService
{
    Task<SoftSwitchResult> SoftActivateAsync(string personaId, CancellationToken cancellationToken = default);

    SettlingSnapshot? GetSettlingSnapshot();
}

public sealed record SoftSwitchResult(
    bool Ok,
    PersonaPack? Persona,
    string? StimulusId,
    string Note,
    int? SettlingTurnsRemaining,
    int? StatusCode = null);
