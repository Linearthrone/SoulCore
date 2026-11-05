namespace SoulCore.Config;

/// <summary>
/// PROP-15.2: per-persona quarantined SQLite / vector path resolution (Avenue B+).
/// Every store path is keyed by <c>personaId</c> under the personas root.
/// </summary>
public static class PersonaMemoryPaths
{
    public const string MemoryFileName = "soulcore_memory.db";
    public const string MemoryDirectoryName = "memory";
    public const string VectorDirectoryName = "vector";

    /// <summary>
    /// <c>{personasRoot}/{personaId}/memory/soulcore_memory.db</c>
    /// (episodic, charter, journal, embeddings live in this file).
    /// </summary>
    public static string ResolveMemoryDbPath(string personasRoot, string personaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personasRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(personaId);
        var id = NormalizePersonaId(personaId);
        return Path.GetFullPath(Path.Combine(personasRoot, id, MemoryDirectoryName, MemoryFileName));
    }

    /// <summary>
    /// Reserved directory for future external vector indexes:
    /// <c>{personasRoot}/{personaId}/vector/</c>.
    /// Current embeddings quarantine via the persona SQLite file above.
    /// </summary>
    public static string ResolveVectorDirectory(string personasRoot, string personaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personasRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(personaId);
        var id = NormalizePersonaId(personaId);
        return Path.GetFullPath(Path.Combine(personasRoot, id, VectorDirectoryName));
    }

    public static string NormalizePersonaId(string personaId)
    {
        var id = personaId.Trim().ToLowerInvariant();
        if (id.Length is < 1 or > 64)
            throw new ArgumentException("personaId length must be 1–64.", nameof(personaId));
        foreach (var ch in id)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
                continue;
            throw new ArgumentException(
                "personaId must be lowercase letters, digits, '-' or '_' only.",
                nameof(personaId));
        }

        return id;
    }
}
