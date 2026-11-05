using SoulCore.Core.Abstractions;
using SoulCore.Memory;

namespace SoulCore.Host.Persona;

/// <summary>
/// DI facades that always forward to the active persona's quarantined stores.
/// Callers keep using existing interfaces; personaId is applied via the hub.
/// </summary>
public sealed class PersonaScopedMemoryStore : IMemoryStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedMemoryStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IMemoryStore Inner => _hub.GetMemoryStore(_hub.ActivePersonaId);

    public bool IsDatabaseOpen => Inner.IsDatabaseOpen;
    public string DatabasePath => Inner.DatabasePath;

    public Task<long> WriteEpisodicAsync(string text, string sourceLabel, CancellationToken cancellationToken = default) =>
        Inner.WriteEpisodicAsync(text, sourceLabel, cancellationToken);

    public Task StoreEmbeddingAsync(long episodicId, float[] vector, string model, CancellationToken cancellationToken = default) =>
        Inner.StoreEmbeddingAsync(episodicId, vector, model, cancellationToken);

    public Task<IReadOnlyList<(long Id, string Content)>> ListEpisodicsMissingEmbeddingsAsync(int limit, CancellationToken cancellationToken = default) =>
        Inner.ListEpisodicsMissingEmbeddingsAsync(limit, cancellationToken);

    public Task<IReadOnlyList<string>> RecallSimilarAsync(float[] queryVector, int limit, CancellationToken cancellationToken = default) =>
        Inner.RecallSimilarAsync(queryVector, limit, cancellationToken);

    public Task<IReadOnlyList<string>> RecallRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        Inner.RecallRecentAsync(limit, cancellationToken);
}

public sealed class PersonaScopedMemoryStats : IMemoryStats
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedMemoryStats(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IMemoryStats Inner => _hub.GetMemoryStats(_hub.ActivePersonaId);

    public bool IsOpen => Inner.IsOpen;

    public Task<long> CountEpisodicAsync(CancellationToken cancellationToken = default) =>
        Inner.CountEpisodicAsync(cancellationToken);
}

public sealed class PersonaScopedEmotionState : IEmotionState
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedEmotionState(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IEmotionState Inner => _hub.GetEmotionState(_hub.ActivePersonaId);

    public Task<IReadOnlyDictionary<string, double>> GetAsync(CancellationToken cancellationToken = default) =>
        Inner.GetAsync(cancellationToken);

    public Task SetAsync(IReadOnlyDictionary<string, double> components, CancellationToken cancellationToken = default) =>
        Inner.SetAsync(components, cancellationToken);

    public Task<long> GetRevisionAsync(CancellationToken cancellationToken = default) =>
        Inner.GetRevisionAsync(cancellationToken);
}

public sealed class PersonaScopedCharter : ICharter
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedCharter(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private ICharter Inner => _hub.GetCharter(_hub.ActivePersonaId);

    public Task<IReadOnlyList<string>> GetAnchorsAsync(CancellationToken cancellationToken = default) =>
        Inner.GetAnchorsAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetAnchorsByKindAsync(
        string kind,
        bool? lockedOnly = null,
        CancellationToken cancellationToken = default) =>
        Inner.GetAnchorsByKindAsync(kind, lockedOnly, cancellationToken);

    public Task<int> SeedAsync(
        IReadOnlyList<CharterAnchorSeed> seeds,
        CancellationToken cancellationToken = default) =>
        Inner.SeedAsync(seeds, cancellationToken);
}

public sealed class PersonaScopedJournalStore : IVictoriaJournalStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedJournalStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IVictoriaJournalStore Inner => _hub.GetJournalStore(_hub.ActivePersonaId);

    public Task<IReadOnlyList<VictoriaJournalBook>> ListBooksAsync(CancellationToken cancellationToken = default) =>
        Inner.ListBooksAsync(cancellationToken);

    public Task<VictoriaJournalBook?> GetBookAsync(string bookId, CancellationToken cancellationToken = default) =>
        Inner.GetBookAsync(bookId, cancellationToken);

    public Task<long> WriteEntryAsync(
        string bookId,
        string body,
        string? moodJson = null,
        string? tagsJson = null,
        string? source = null,
        string? occurredAt = null,
        CancellationToken cancellationToken = default) =>
        Inner.WriteEntryAsync(bookId, body, moodJson, tagsJson, source, occurredAt, cancellationToken);

    public Task<IReadOnlyList<VictoriaJournalEntry>> ListEntriesAsync(
        string? bookId = null,
        int limit = 20,
        CancellationToken cancellationToken = default) =>
        Inner.ListEntriesAsync(bookId, limit, cancellationToken);
}

public sealed class PersonaScopedTaskStore : IVictoriaTaskStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedTaskStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IVictoriaTaskStore Inner => _hub.GetTaskStore(_hub.ActivePersonaId);

    public Task<long> CreateAsync(string title, string? description, string? priority, CancellationToken cancellationToken = default) =>
        Inner.CreateAsync(title, description, priority, cancellationToken);

    public Task<long> CreateAsync(
        string title,
        string? description,
        string? priority,
        string? surface,
        bool ready,
        string? status,
        string? notes,
        CancellationToken cancellationToken = default) =>
        Inner.CreateAsync(title, description, priority, surface, ready, status, notes, cancellationToken);

    public Task<VictoriaTask?> GetAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.GetAsync(id, cancellationToken);

    public Task<bool> UpdateStatusAsync(long id, string status, CancellationToken cancellationToken = default) =>
        Inner.UpdateStatusAsync(id, status, cancellationToken);

    public Task<bool> ParkAsync(
        long id,
        string reason,
        DateTimeOffset parkUntil,
        CancellationToken cancellationToken = default) =>
        Inner.ParkAsync(id, reason, parkUntil, cancellationToken);

    public Task<bool> ReleaseAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.ReleaseAsync(id, cancellationToken);

    public Task<bool> SuspendAsync(long id, string? notes = null, CancellationToken cancellationToken = default) =>
        Inner.SuspendAsync(id, notes, cancellationToken);

    public Task<int> RecordIdenticalFailureAsync(
        long id,
        string failureKey,
        CancellationToken cancellationToken = default) =>
        Inner.RecordIdenticalFailureAsync(id, failureKey, cancellationToken);

    public Task ClearFailureCounterAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.ClearFailureCounterAsync(id, cancellationToken);

    public Task<bool> UpdateDetailsAsync(
        long id,
        string? surface = null,
        string? notes = null,
        CancellationToken cancellationToken = default) =>
        Inner.UpdateDetailsAsync(id, surface, notes, cancellationToken);

    public Task<IReadOnlyList<VictoriaTask>> ListAsync(string? status = null, CancellationToken cancellationToken = default) =>
        Inner.ListAsync(status, cancellationToken);

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.DeleteAsync(id, cancellationToken);
}

public sealed class PersonaScopedWorkflowStore : IVictoriaWorkflowStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedWorkflowStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IVictoriaWorkflowStore Inner => _hub.GetWorkflowStore(_hub.ActivePersonaId);

    public Task<long> CreateAsync(string name, IReadOnlyList<WorkflowStep> steps, CancellationToken cancellationToken = default) =>
        Inner.CreateAsync(name, steps, cancellationToken);

    public Task<VictoriaWorkflow?> GetAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.GetAsync(id, cancellationToken);

    public Task<bool> SetCurrentStepAsync(long id, int currentStep, CancellationToken cancellationToken = default) =>
        Inner.SetCurrentStepAsync(id, currentStep, cancellationToken);
}

public sealed class PersonaScopedSopStore : IVictoriaSopStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedSopStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IVictoriaSopStore Inner => _hub.GetSopStore(_hub.ActivePersonaId);

    public Task<long> CreateAsync(
        string title,
        string whenText,
        string steps,
        string donts,
        CancellationToken cancellationToken = default) =>
        Inner.CreateAsync(title, whenText, steps, donts, cancellationToken);

    public Task<VictoriaSop?> GetAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.GetAsync(id, cancellationToken);

    public Task<bool> UpdateAsync(
        long id,
        string? title = null,
        string? whenText = null,
        string? steps = null,
        string? donts = null,
        CancellationToken cancellationToken = default) =>
        Inner.UpdateAsync(id, title, whenText, steps, donts, cancellationToken);

    public Task<IReadOnlyList<VictoriaSop>> ListAsync(CancellationToken cancellationToken = default) =>
        Inner.ListAsync(cancellationToken);
}

public sealed class PersonaScopedResearchStore : IVictoriaResearchStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedResearchStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IVictoriaResearchStore Inner => _hub.GetResearchStore(_hub.ActivePersonaId);

    public Task<IReadOnlyList<VictoriaResearchSubject>> ListSubjectsAsync(
        CancellationToken cancellationToken = default) =>
        Inner.ListSubjectsAsync(cancellationToken);

    public Task<VictoriaResearchSubject?> GetSubjectAsync(
        long id,
        CancellationToken cancellationToken = default) =>
        Inner.GetSubjectAsync(id, cancellationToken);

    public Task<VictoriaResearchSubject?> GetSubjectBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        Inner.GetSubjectBySlugAsync(slug, cancellationToken);

    public Task<VictoriaResearchSubject> OpenOrCreateSubjectAsync(
        string title,
        string? summary = null,
        CancellationToken cancellationToken = default) =>
        Inner.OpenOrCreateSubjectAsync(title, summary, cancellationToken);

    public Task<bool> TouchSubjectAsync(long id, CancellationToken cancellationToken = default) =>
        Inner.TouchSubjectAsync(id, cancellationToken);

    public Task<IReadOnlyList<VictoriaResearchPaper>> ListPapersAsync(
        long? subjectId = null,
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        Inner.ListPapersAsync(subjectId, limit, cancellationToken);

    public Task<VictoriaResearchPaper?> GetPaperAsync(
        long id,
        CancellationToken cancellationToken = default) =>
        Inner.GetPaperAsync(id, cancellationToken);

    public Task<VictoriaResearchPaper> UpsertPaperAsync(
        long subjectId,
        string title,
        string kind,
        string? abstractText,
        string? bodyApa,
        string? bibliographyApa,
        string? status,
        long? paperId = null,
        CancellationToken cancellationToken = default) =>
        Inner.UpsertPaperAsync(
            subjectId, title, kind, abstractText, bodyApa, bibliographyApa, status, paperId, cancellationToken);
}

public sealed class PersonaScopedTranscriptStore : IChatTranscriptStore
{
    private readonly IPersonaStoreHub _hub;

    public PersonaScopedTranscriptStore(IPersonaStoreHub hub) =>
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

    private IChatTranscriptStore Inner => _hub.GetTranscriptStore(_hub.ActivePersonaId);

    public Task<long> AppendAsync(
        string conversationId,
        string role,
        string content,
        string? channel = null,
        string? frameId = null,
        string? mediaId = null,
        CancellationToken cancellationToken = default) =>
        Inner.AppendAsync(conversationId, role, content, channel, frameId, mediaId, cancellationToken);

    public Task<IReadOnlyList<ChatTranscriptMessage>> GetSinceAsync(
        string conversationId,
        long afterId,
        int limit,
        CancellationToken cancellationToken = default) =>
        Inner.GetSinceAsync(conversationId, afterId, limit, cancellationToken);

    public Task<IReadOnlyList<ChatTranscriptMessage>> GetRecentAsync(
        string conversationId,
        int limit,
        CancellationToken cancellationToken = default) =>
        Inner.GetRecentAsync(conversationId, limit, cancellationToken);

    public Task<long> GetLatestCursorAsync(string conversationId, CancellationToken cancellationToken = default) =>
        Inner.GetLatestCursorAsync(conversationId, cancellationToken);
}
