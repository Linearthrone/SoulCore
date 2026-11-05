using Microsoft.Extensions.Logging.Abstractions;
using SoulCore.Config;
using SoulCore.Core.Charter;
using SoulCore.Memory;
using SoulCore.Memory.Repositories;

namespace SoulCore.Host.Persona;

/// <summary>
/// One persona's open SQLite session + focused repos + charter connection.
/// Disposed on active-persona switch (PROP-15.2 / PROP-20.1 quarantine).
/// </summary>
internal sealed class PersonaStoreBundle : IAsyncDisposable, IDisposable
{
    public PersonaStoreBundle(string personaId, string personasRoot)
    {
        PersonaId = PersonaMemoryPaths.NormalizePersonaId(personaId);
        DatabasePath = PersonaMemoryPaths.ResolveMemoryDbPath(personasRoot, PersonaId);
        VectorDirectory = PersonaMemoryPaths.ResolveVectorDirectory(personasRoot, PersonaId);

        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(VectorDirectory);

        Session = new SqliteMemorySession(DatabasePath, NullLogger<SqliteMemorySession>.Instance);
        Episodic = new SqliteEpisodicMemoryRepository(Session);
        Emotion = new SqliteEmotionRepository(Session);
        Tasks = new SqliteVictoriaTaskRepository(Session);
        Workflows = new SqliteVictoriaWorkflowRepository(Session);
        Journals = new SqliteVictoriaJournalRepository(Session);
        Sops = new SqliteVictoriaSopRepository(Session);
        Research = new SqliteVictoriaResearchRepository(Session);
        Transcript = new SqliteChatTranscriptRepository(Session);
        Charter = new CharterService(DatabasePath);
    }

    public string PersonaId { get; }
    public string DatabasePath { get; }
    public string VectorDirectory { get; }
    public SqliteMemorySession Session { get; }
    public SqliteEpisodicMemoryRepository Episodic { get; }
    public SqliteEmotionRepository Emotion { get; }
    public SqliteVictoriaTaskRepository Tasks { get; }
    public SqliteVictoriaWorkflowRepository Workflows { get; }
    public SqliteVictoriaJournalRepository Journals { get; }
    public SqliteVictoriaSopRepository Sops { get; }
    public SqliteVictoriaResearchRepository Research { get; }
    public SqliteChatTranscriptRepository Transcript { get; }
    public CharterService Charter { get; }

    public void Dispose()
    {
        Charter.Dispose();
        Session.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Charter.DisposeAsync().ConfigureAwait(false);
        await Session.DisposeAsync().ConfigureAwait(false);
    }
}
