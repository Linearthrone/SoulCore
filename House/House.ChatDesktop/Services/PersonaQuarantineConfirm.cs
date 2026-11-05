namespace House.ChatDesktop.Services;

/// <summary>Copy for persona switch / edit quarantine — memories do not cross packs.</summary>
public static class PersonaQuarantineConfirm
{
    public static string BuildMessage(string displayName, string personaId)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? personaId : displayName.Trim();
        var id = string.IsNullOrWhiteSpace(personaId) ? "?" : personaId.Trim();
        return
            $"Switch active persona to {name} ({id})?\n\n" +
            "Memories, charter, and journals stay quarantined per persona. " +
            "They do not follow this switch.\n\n" +
            "Continue?";
    }

    /// <summary>PROP-15.3 edit wizard — remind operator quarantine is per-persona.</summary>
    public static string BuildEditQuarantineNotice(string displayName, string personaId)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? personaId : displayName.Trim();
        var id = string.IsNullOrWhiteSpace(personaId) ? "?" : personaId.Trim();
        return
            $"Editing {name} ({id}) only. Memories, charter, and journals stay quarantined " +
            "to this persona — they do not follow a later switch.";
    }
}
