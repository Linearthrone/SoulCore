namespace SoulCore.Config;

/// <summary>PROP-20.2 path helpers for named shared spaces.</summary>
public static class SharedSpacePaths
{
    public const string DbFileName = "space.db";
    public const string MetaFileName = "space.json";

    public static string NormalizeSpaceId(string spaceId)
    {
        var id = spaceId.Trim().ToLowerInvariant();
        if (id.Length is < 1 or > 64)
            throw new ArgumentException("spaceId length must be 1–64.", nameof(spaceId));
        foreach (var ch in id)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
                continue;
            throw new ArgumentException(
                "spaceId must be lowercase letters, digits, '-' or '_' only.",
                nameof(spaceId));
        }

        return id;
    }

    public static string ResolveSpaceDirectory(string spacesRoot, string spaceId) =>
        Path.GetFullPath(Path.Combine(spacesRoot, NormalizeSpaceId(spaceId)));

    public static string ResolveDbPath(string spacesRoot, string spaceId) =>
        Path.Combine(ResolveSpaceDirectory(spacesRoot, spaceId), DbFileName);

    public static string ResolveMetaPath(string spacesRoot, string spaceId) =>
        Path.Combine(ResolveSpaceDirectory(spacesRoot, spaceId), MetaFileName);
}
