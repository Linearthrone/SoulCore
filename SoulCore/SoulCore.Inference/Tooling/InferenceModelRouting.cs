using SoulCore.Config;
using SoulCore.Core.Persona;

namespace SoulCore.Inference.Tooling;

/// <summary>
/// Resolves chat / tool / embed Ollama model names from <see cref="InferenceOptions"/>
/// (and optional active <see cref="PersonaPack.InferenceModel"/>) plus whether Unreal
/// is currently live (VRAM policy).
/// </summary>
public static class InferenceModelRouting
{
    /// <summary>
    /// Chat model: pack <see cref="PersonaPack.InferenceModel"/> when set, else
    /// Host <c>Inference:Model</c>, else <c>gemma4:latest</c>.
    /// </summary>
    public static string ResolveChatModel(InferenceOptions options, string? packInferenceModel = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var fromPack = (packInferenceModel ?? string.Empty).Trim();
        if (fromPack.Length > 0)
            return fromPack;
        return FirstNonEmpty(options.Model, "gemma4:latest");
    }

    /// <summary>
    /// Chat model for a framed persona id (v1: active only via <see cref="IPersonaSession.TryGet"/>).
    /// </summary>
    public static string ResolveChatModel(
        InferenceOptions options,
        IPersonaSession? session,
        string? personaId = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ResolveChatModel(options, ResolvePackInferenceModel(session, personaId));
    }

    /// <summary>
    /// Tool-loop model: <see cref="InferenceOptions.ToolModelUeLive"/> when UE live,
    /// else <see cref="InferenceOptions.ToolModel"/>, else chat model (pack → Host).
    /// </summary>
    public static string ResolveToolModel(
        InferenceOptions options,
        bool ueLive,
        string? packInferenceModel = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (ueLive && !string.IsNullOrWhiteSpace(options.ToolModelUeLive))
            return options.ToolModelUeLive.Trim();
        if (!string.IsNullOrWhiteSpace(options.ToolModel))
            return options.ToolModel.Trim();
        return ResolveChatModel(options, packInferenceModel);
    }

    /// <summary>
    /// Tool-loop model for a framed persona id (v1: active only).
    /// </summary>
    public static string ResolveToolModel(
        InferenceOptions options,
        bool ueLive,
        IPersonaSession? session,
        string? personaId = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ResolveToolModel(options, ueLive, ResolvePackInferenceModel(session, personaId));
    }

    /// <summary>
    /// Pack inference model for <paramref name="personaId"/> when warm, else active pack.
    /// </summary>
    public static string? ResolvePackInferenceModel(IPersonaSession? session, string? personaId = null)
    {
        if (session is null)
            return null;

        if (!string.IsNullOrWhiteSpace(personaId)
            && session.TryGet(personaId, out var pack))
            return pack.InferenceModel;

        return session.GetActive().InferenceModel;
    }

    /// <summary>
    /// PROP-17.3: leisure / R&amp;R model. Returns <c>null</c> when
    /// <see cref="InferenceOptions.LeisureModel"/> is empty — caller must stay quiet.
    /// Never falls back to chat or tool model.
    /// </summary>
    public static string? ResolveLeisureModel(InferenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.LeisureModel))
            return null;
        return options.LeisureModel.Trim();
    }

    public static string ResolveEmbeddingModel(InferenceOptions options, bool ueLive)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (ueLive && !string.IsNullOrWhiteSpace(options.EmbeddingModelUeLive))
            return options.EmbeddingModelUeLive.Trim();
        return FirstNonEmpty(options.EmbeddingModel, "nomic-embed-text");
    }

    /// <summary>
    /// Tool-loop <c>num_ctx</c>: prefer <see cref="InferenceOptions.ToolNumCtxUeLive"/> when UE live.
    /// </summary>
    public static int ResolveToolNumCtx(InferenceOptions options, bool ueLive)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (ueLive && options.ToolNumCtxUeLive > 0)
            return options.ToolNumCtxUeLive;
        return options.NumCtx;
    }

    public static bool ShouldSkipEmbeddings(InferenceOptions options, bool ueLive)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ueLive && options.SkipEmbeddingsWhenUeLive;
    }

    private static string FirstNonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
