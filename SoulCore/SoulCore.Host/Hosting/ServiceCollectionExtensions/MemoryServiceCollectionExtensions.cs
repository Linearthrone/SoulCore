using Microsoft.Extensions.DependencyInjection;
using SoulCore.Config;
using SoulCore.Inference.Clients;
using SoulCore.Inference.Tooling;
using SoulCore.Core.Abstractions;
using SoulCore.Core.Safety;
using SoulCore.Inference.Tools;
using SoulCore.Host.Persona;
using SoulCore.Memory;

namespace SoulCore.Host.Hosting.ServiceCollectionExtensions;

internal static class MemoryServiceCollectionExtensions
{
    /// <summary>
    /// PROP-20.1: memory / charter / journals are persona-quarantined via
    /// <see cref="IPersonaStoreHub"/>. Call <c>AddPersonaRuntime()</c> before this.
    /// </summary>
    internal static IServiceCollection AddMemory(
        this IServiceCollection services,
        MemoryOptions memoryOptions,
        SafetyOptions safetyOptions)
    {
        // memoryOptions kept for API compatibility / future migrate hooks; stores
        // resolve paths from PersonaOptions via the hub.
        _ = memoryOptions;

        services.AddSingleton<IMemoryStore, PersonaScopedMemoryStore>();
        services.AddSingleton<IMemoryStats, PersonaScopedMemoryStats>();
        services.AddSingleton<IEmotionState, PersonaScopedEmotionState>();
        services.AddSingleton<IVictoriaTaskStore, PersonaScopedTaskStore>();
        services.AddSingleton<IVictoriaWorkflowStore, PersonaScopedWorkflowStore>();
        services.AddSingleton<IVictoriaJournalStore, PersonaScopedJournalStore>();
        services.AddSingleton<IVictoriaSopStore, PersonaScopedSopStore>();
        services.AddSingleton<IVictoriaResearchStore, PersonaScopedResearchStore>();
        services.AddSingleton<IChatTranscriptStore, PersonaScopedTranscriptStore>();
        services.AddSingleton<ICharter, PersonaScopedCharter>();

        services.AddSingleton<DriftWatcher>(_ => new DriftWatcher(safetyOptions.DriftSloMinutes));
        services.AddSingleton<SpendMeter>(_ => new SpendMeter(
            safetyOptions.InputTokenRatePer1K,
            safetyOptions.OutputTokenRatePer1K,
            safetyOptions.MonthlyCapUsd,
            safetyOptions.MonthlyTokenCap));

        services.AddSingleton<ITool, RecallMemoryTool>();
        services.AddSingleton<ITool, StoreMemoryTool>();

        return services;
    }
}
