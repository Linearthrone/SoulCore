namespace SoulCore.Core.SharedSpaces;

/// <summary>
/// Named shared knowledge space (PROP-20.2). Separate store from persona private DBs.
/// Personas attach via <c>PersonaPack.SharedSpaceIds</c>.
/// </summary>
public sealed class SharedSpace
{
    public const int NameMaxLength = 120;
    public const int IdMaxLength = 64;

    public string SpaceId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional operator note; not injected into prompts.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public SharedSpace Clone() => new()
    {
        SpaceId = SpaceId,
        DisplayName = DisplayName,
        Description = Description,
        CreatedAtUtc = CreatedAtUtc,
        UpdatedAtUtc = UpdatedAtUtc
    };
}
