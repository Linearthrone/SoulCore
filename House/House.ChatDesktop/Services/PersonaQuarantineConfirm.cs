namespace House.ChatDesktop.Services;

/// <summary>Copy for persona switch / edit quarantine — memories do not cross packs.</summary>
public static class PersonaQuarantineConfirm
{
    public static string BuildMessage(string displayName, string personaId)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? personaId : displayName.Trim();
        var id = string.IsNullOrWhiteSpace(personaId) ? "?" : personaId.Trim();
        return
            $"Soft-switch to {name} ({id})?\n\n" +
            "This is a soft handoff (never a hard cut): the outgoing mind journals a close, " +
            "their recent turns stay archive of another mind, and the incoming mind settles for a few turns.\n\n" +
            "Memories, charter, and journals stay quarantined per persona — they do not follow this switch.\n\n" +
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
