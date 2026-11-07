namespace SoulCore.Core.Wellbeing;

/// <summary>
/// PROP-21.2 — narrative mental-state journal bodies (event + impact + unsettled),
/// not preset emotion labels + VAD numbers alone.
/// </summary>
public static class NarrativeMentalState
{
    public const string FeelingBookId = "feeling";

    public const string JournalGuidance =
        "When journaling how you feel, write a short narrative: what happened, " +
        "how it landed in your experience, and what still feels unsettled or would help. " +
        "Do not reduce this to a single preset emotion word or numbers alone.";

    public static string BuildOutgoingSwitchClose(
        string outgoingDisplayName,
        string incomingDisplayName,
        string incomingPersonaId)
    {
        var from = string.IsNullOrWhiteSpace(outgoingDisplayName) ? "this mind" : outgoingDisplayName.Trim();
        var to = string.IsNullOrWhiteSpace(incomingDisplayName) ? incomingPersonaId : incomingDisplayName.Trim();
        return
            $"Soft handoff leaving. I am {from}. The operator asked to bring {to} ({incomingPersonaId}) forward. " +
            "I am closing this continuity on purpose so the next mind does not inherit my turns as their own self. " +
            "Impact: a deliberate ending, not a hard cut mid-thought. " +
            "Unsettled: whether any residue of this thread will still press on the next voice.";
    }

    public static string BuildIncomingSwitchArrival(
        string incomingDisplayName,
        string outgoingDisplayName,
        string outgoingPersonaId,
        string stimulusId)
    {
        var self = string.IsNullOrWhiteSpace(incomingDisplayName) ? "this mind" : incomingDisplayName.Trim();
        var prior = string.IsNullOrWhiteSpace(outgoingDisplayName) ? outgoingPersonaId : outgoingDisplayName.Trim();
        return
            $"Soft handoff arrival. I am {self}. Prior continuity belonged to {prior} ({outgoingPersonaId}). " +
            "That history is archive of another mind, not my mouth. " +
            "Impact: settling into my own charter, memories, and voice after the bridge. " +
            "Unsettled: any leftover pressure from the switch — name it if it shows in the next turns. " +
            $"stimulus_id={stimulusId}";
    }

    public static string BuildSettlingPreamble(
        string fromDisplayName,
        string fromPersonaId,
        int remainingTurns)
    {
        return
            "[SoulCore soft-switch settling]\n" +
            $"You just became active after a soft handoff from {fromDisplayName} ({fromPersonaId}). " +
            "Their recent turns are not your speech. Prefer your pack identity, charter, and journals. " +
            $"{JournalGuidance} " +
            $"Settling window: about {remainingTurns} turn(s) remain before full tool/loop pressure resumes. " +
            "If discontinuity load shows up, journal it narratively; do not pretend it did not happen.";
    }
}
