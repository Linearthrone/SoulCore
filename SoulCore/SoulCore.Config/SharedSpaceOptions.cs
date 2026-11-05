namespace SoulCore.Config;

/// <summary>PROP-20.2 named shared-space root under LocalAppData/SoulCore/shared-spaces.</summary>
public sealed class SharedSpaceOptions
{
    public const string SectionName = "SharedSpaces";

    /// <summary>Empty → <c>%LOCALAPPDATA%/SoulCore/shared-spaces</c>.</summary>
    public string RootDirectory { get; set; } = string.Empty;

    public static string ResolveDefaultRootDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "SoulCore", "shared-spaces");
    }

    public string ResolveRootDirectory()
    {
        if (string.IsNullOrWhiteSpace(RootDirectory))
            return ResolveDefaultRootDirectory();
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(RootDirectory.Trim()));
    }
}
