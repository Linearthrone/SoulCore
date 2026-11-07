namespace SoulCore.Config;

/// <summary>PROP-22 — house-level observatory paths under the personas root.</summary>
public static class ObservatoryPaths
{
    public const string DirectoryName = "observatory";
    public const string DatabaseFileName = "stimuli.db";

    public static string ResolveDirectory(string personasRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personasRoot);
        return Path.GetFullPath(Path.Combine(personasRoot, DirectoryName));
    }

    public static string ResolveDatabasePath(string personasRoot) =>
        Path.GetFullPath(Path.Combine(ResolveDirectory(personasRoot), DatabaseFileName));
}
