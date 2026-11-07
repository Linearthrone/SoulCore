using System.Text.RegularExpressions;

namespace SoulCore.Core.Persona;

/// <summary>
/// Detects an explicit human-name correction in chat/SMS text so the Host can
/// persist <see cref="PersonaPack.HumanAddress"/> across restarts.
/// Memory and transcript may still contain obsolete names; the pack is SoT.
/// </summary>
public static partial class HumanAddressCorrection
{
    /// <summary>Fallback when pack address is blank.</summary>
    public const string DefaultAddress = "Friend";

    public static string Normalize(string? humanAddress)
    {
        if (string.IsNullOrWhiteSpace(humanAddress))
            return DefaultAddress;
        return humanAddress.Trim();
    }

    /// <summary>
    /// Extracts a new standing address when the human clearly renames themselves.
    /// Returns null when no correction is found (including "stop calling me X" alone).
    /// </summary>
    public static string? TryExtractNewAddress(string? userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return null;

        var text = userText.Trim();

        foreach (var regex in CorrectionPatterns())
        {
            var match = regex.Match(text);
            if (!match.Success)
                continue;

            var name = match.Groups["name"].Value.Trim().TrimEnd('.', '!', '?', ',', ';', ':');
            if (!IsPlausibleAddress(name))
                continue;

            return name;
        }

        return null;
    }

    /// <summary>True when <paramref name="candidate"/> differs from the current standing address.</summary>
    public static bool IsChange(string? currentAddress, string newAddress)
    {
        var current = Normalize(currentAddress);
        var next = Normalize(newAddress);
        return !string.Equals(current, next, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlausibleAddress(string name)
    {
        if (name.Length is < 2 or > 40)
            return false;
        if (name.Contains(' ', StringComparison.Ordinal) && name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 3)
            return false;
        // Reject obvious sentence fragments.
        if (name.Contains(" please", StringComparison.OrdinalIgnoreCase)
            || name.Contains(" instead", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static IEnumerable<Regex> CorrectionPatterns()
    {
        // Prefer "stop calling me X, call me Y" before a bare "call me …"
        // so "don't call me OldName call me NewName" keeps NewName.
        yield return StopCallingMeXCallMeYRegex();
        yield return NotXCallMeYRegex();
        yield return MyNameIsRegex();
        yield return AddressMeAsRegex();
        yield return IGoByRegex();
        yield return CallMeRegex();
    }

    [GeneratedRegex(
        @"(?<!(?:don'?t|do\s+not|never|stop)\s+)(?:please\s+)?(?:just\s+)?call\s+me\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CallMeRegex();

    [GeneratedRegex(
        @"\bmy\s+name\s+is\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyNameIsRegex();

    [GeneratedRegex(
        @"\baddress\s+me\s+as\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AddressMeAsRegex();

    [GeneratedRegex(
        @"\bi\s+go\s+by\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IGoByRegex();

    [GeneratedRegex(
        @"\b(?:it'?s|use)\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\s*,?\s+not\s+[A-Za-z]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NotXCallMeYRegex();

    [GeneratedRegex(
        @"\b(?:stop|don'?t|do\s+not)\s+call(?:ing)?\s+me\s+[A-Za-z][A-Za-z0-9'\-]{1,39}\b.{0,40}?\b(?:call\s+me|use|address\s+me\s+as)\s+(?<name>[A-Za-z][A-Za-z0-9'\-]{1,39})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex StopCallingMeXCallMeYRegex();
}
