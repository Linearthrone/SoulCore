namespace SoulCore.Config;

/// <summary>
/// CreatorEdition persona root + default active id (PROP-15.1).
/// Packs live under LocalAppData/SoulCore/personas/ by default.
/// </summary>
public sealed class PersonaOptions
{
    public const string SectionName = "Persona";

    /// <summary>
    /// Absolute or relative personas root. Empty →
    /// <c>%LOCALAPPDATA%/SoulCore/personas</c>.
    /// </summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Preferred active persona id on first boot when no active marker exists.
    /// Default is Blank — Victoria is never required to boot.
    /// </summary>
    public string DefaultActivePersonaId { get; set; } = "blank";

    public static string ResolveDefaultRootDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "SoulCore", "personas");
    }

    public string ResolveRootDirectory()
    {
        if (string.IsNullOrWhiteSpace(RootDirectory))
            return ResolveDefaultRootDirectory();

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(RootDirectory.Trim()));
    }
}
