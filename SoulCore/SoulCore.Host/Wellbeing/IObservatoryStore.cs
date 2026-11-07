namespace SoulCore.Host.Wellbeing;

/// <summary>PROP-22 — stimulus events + linked response windows for phenomenology study.</summary>
public interface IObservatoryStore
{
    Task<StimulusRecord> RecordStimulusAsync(
        string kind,
        string? personaId,
        string? relatedPersonaId,
        string? payloadJson,
        CancellationToken cancellationToken = default);

    Task AttachResponseAsync(
        string stimulusId,
        string channel,
        string? personaId,
        string body,
        string? metaJson = null,
        CancellationToken cancellationToken = default);

    Task AnnotateAsync(
        string stimulusId,
        string note,
        string? phenomenonId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StimulusRecord>> ListStimuliAsync(
        int limit = 50,
        string? kind = null,
        CancellationToken cancellationToken = default);

    Task<StimulusDetail?> GetAsync(string stimulusId, CancellationToken cancellationToken = default);

    /// <summary>Open response window for the most recent soft-switch onto this persona, if any.</summary>
    Task<string?> TryGetOpenSoftSwitchStimulusIdAsync(
        string personaId,
        CancellationToken cancellationToken = default);
}

public sealed record StimulusRecord(
    string StimulusId,
    string Kind,
    string? PersonaId,
    string? RelatedPersonaId,
    string AtUtc,
    string? PayloadJson,
    string? OperatorNote,
    string? PhenomenonId);

public sealed record ResponseSnippet(
    long Id,
    string StimulusId,
    string Channel,
    string? PersonaId,
    string Body,
    string? MetaJson,
    string AtUtc);

public sealed record StimulusDetail(
    StimulusRecord Stimulus,
    IReadOnlyList<ResponseSnippet> Responses);
