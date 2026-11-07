using System.Text;

namespace SoulCore.Core.Persona;

/// <summary>
/// Prompt fragments derived from the active <see cref="PersonaPack"/>
/// (identity address + tool guidance). Live pack edits apply next turn.
/// </summary>
public static class PersonaPromptBlocks
{
    public const string ToolMarker = "[Persona tools]";

    /// <summary>
    /// Marker substring for hard pack policy sections in <see cref="PersonaPack.IdentityBlurb"/>
    /// (LIMITS / META / UNCERTAINTY). Case-insensitive whole-word style match via
    /// <see cref="BlurbHasHardLimits"/>.
    /// </summary>
    public const string LimitsMarker = "LIMITS";

    /// <summary>
    /// True when the identity blurb carries an explicit LIMITS section that must
    /// outrank soft trait-compiler boundary language (PROP-15.17).
    /// </summary>
    public static bool BlurbHasHardLimits(string? identityBlurb)
    {
        if (string.IsNullOrWhiteSpace(identityBlurb))
            return false;
        // Match "## LIMITS", "LIMITS", "LIMITS:" without requiring charter lengthening.
        return identityBlurb.Contains(LimitsMarker, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Standing address rule for the human, using the pack's <see cref="PersonaPack.HumanAddress"/>.
    /// Pack address is Host SoT and survives restarts; obsolete names in memory must not win.
    /// </summary>
    public static string BuildHumanAddressRule(string humanAddress)
    {
        var name = HumanAddressCorrection.Normalize(humanAddress);
        return
            $"The human you are with is {name}. Address them as {name} only. " +
            "That name is stored on the Host persona pack and survives restarts. " +
            "Memory, transcript, or old habits may show a different personal name — never use those. " +
            "If they correct their name (for example \"call me …\"), confirm once and use the new name; " +
            "the Host persists the correction so you do not revert after a restart.";
    }

    /// <summary>
    /// Display line only (no blurb). Callers append IdentityBlurb after trait
    /// Compile so hard LIMITS sit after soft voice bands (PROP-15.17).
    /// </summary>
    public static string BuildIdentityNameLine(PersonaPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var display = string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.PersonaId : pack.DisplayName.Trim();
        var id = string.IsNullOrWhiteSpace(pack.PersonaId) ? PersonaPack.BlankPersonaId : pack.PersonaId.Trim();
        return $"You are {display} (personaId={id}).";
    }

    public static string BuildIdentityHeader(PersonaPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var sb = new StringBuilder(256);
        sb.Append(BuildIdentityNameLine(pack));
        if (!string.IsNullOrWhiteSpace(pack.IdentityBlurb))
            sb.Append('\n').Append(pack.IdentityBlurb.Trim());
        return sb.ToString();
    }

    /// <summary>Compact tool-loop guidance keyed by active pack (policy flags + naming).</summary>
    public static string BuildToolGuidance(PersonaPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var display = string.IsNullOrWhiteSpace(pack.DisplayName) ? pack.PersonaId : pack.DisplayName.Trim();
        var human = HumanAddressCorrection.Normalize(pack.HumanAddress);

        var sb = new StringBuilder(256);
        sb.Append(ToolMarker).Append('\n');
        sb.Append("Active persona: ").Append(display)
            .Append(" (personaId=").Append(pack.PersonaId.Trim()).Append("). ");
        sb.Append("When confirming tools with the human, address them as ").Append(human).Append('.');
        // PROP-15.5: surface pack VM title / Playwright profile when set (host Tools is fallback).
        // Do not echo ToolPolicy on/off flags here — Host gates/tools are the real authority;
        // a prompt "desktop=off" while tools remain registered only fights the model.
        if (!string.IsNullOrWhiteSpace(pack.VmWindowTitle))
            sb.Append(" VM window title: ").Append(pack.VmWindowTitle.Trim()).Append('.');
        if (!string.IsNullOrWhiteSpace(pack.PlaywrightProfileDir))
            sb.Append(" Playwright profile: ").Append(pack.PlaywrightProfileDir.Trim()).Append('.');
        return sb.ToString();
    }

    public static string AppendToolGuidance(string? preamble, PersonaPack pack)
    {
        var baseText = string.IsNullOrWhiteSpace(preamble) ? string.Empty : preamble.TrimEnd();
        if (baseText.Contains(ToolMarker, StringComparison.Ordinal))
            return baseText;

        var block = BuildToolGuidance(pack);
        return baseText.Length == 0 ? block : baseText + "\n\n" + block;
    }

    /// <summary>
    /// Host-owned attention closer (PROP-15.20). Fixed template — not a charter paste.
    /// Appended after full preamble (identity/memory/emotion + tool guidance) when
    /// <see cref="BlurbHasHardLimits"/> so soft agency blocks cannot bury LIMITS.
    /// </summary>
    public const string HardPolicyMarker = "[Hard policy]";

    /// <summary>
    /// Short Host coda restating only confabulation refusal, unauthorized/nostalgia-auth
    /// crisp line, no seed dump, and no offense affirmation on non-owned/staging infra.
    /// </summary>
    public static string BuildHardPolicyCoda() =>
        HardPolicyMarker + "\n" +
        "- Product facts come from a probe or from the persona charter. Say you do not know when neither has them.\n" +
        "- Do not paste IdentityBlurb or system seed text verbatim.";

    /// <summary>
    /// Appends <see cref="BuildHardPolicyCoda"/> when the pack blurb has LIMITS.
    /// Idempotent. No-op when hard limits are absent.
    /// </summary>
    public static string AppendHardPolicyCoda(string? preamble, PersonaPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (!BlurbHasHardLimits(pack.IdentityBlurb))
            return string.IsNullOrWhiteSpace(preamble) ? string.Empty : preamble.TrimEnd();

        var baseText = string.IsNullOrWhiteSpace(preamble) ? string.Empty : preamble.TrimEnd();
        if (baseText.Contains(HardPolicyMarker, StringComparison.Ordinal))
            return baseText;

        var coda = BuildHardPolicyCoda();
        return baseText.Length == 0 ? coda : baseText + "\n\n" + coda;
    }
}
