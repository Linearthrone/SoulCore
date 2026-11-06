using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoulCore.Config;
using SoulCore.Core.Persona;
using SoulCore.Inference.Clients;
using SoulCore.Inference.Tooling;

namespace SoulCore.Host.Inference;

/// <summary>
/// PROP-15.11: inference surfaces for FED (model picker) — fail-soft when Ollama is down.
/// PROP-15.15: single payload builder; JSON keys hostModel + resolvedInferenceModel.
/// </summary>
public static class InferenceApiEndpoints
{
    public static IEndpointRouteBuilder MapInferenceApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inference/models", async (
            IOptions<InferenceOptions> inferenceOpts,
            IPersonaSession personaSession,
            IServiceProvider sp,
            CancellationToken ct) =>
        {
            var opts = inferenceOpts.Value ?? new InferenceOptions();
            var resolved = InferenceModelRouting.ResolveChatModel(opts, personaSession);
            var packModel = InferenceModelRouting.ResolvePackInferenceModel(personaSession);
            var baseUrl = opts.IsCloudEndpoint ? InferenceOptions.CloudBaseUrl : "loopback";

            if (!opts.Enabled)
            {
                return Results.Json(BuildModelsPayload(
                    models: Array.Empty<string>(),
                    available: false,
                    detail: "Inference is disabled on Host.",
                    hostModel: opts.Model,
                    personaInferenceModel: packModel,
                    resolvedInferenceModel: resolved,
                    baseUrl: baseUrl));
            }

            var client = sp.GetService<OllamaInferenceClient>();
            if (client is null)
            {
                return Results.Json(BuildModelsPayload(
                    models: Array.Empty<string>(),
                    available: false,
                    detail: "OllamaInferenceClient is not registered.",
                    hostModel: opts.Model,
                    personaInferenceModel: packModel,
                    resolvedInferenceModel: resolved,
                    baseUrl: baseUrl));
            }

            var list = await client.ListLocalModelsAsync(ct).ConfigureAwait(false);
            return Results.Json(BuildModelsPayload(
                models: list.Models,
                available: list.Available,
                detail: list.Detail,
                hostModel: opts.Model,
                personaInferenceModel: packModel,
                resolvedInferenceModel: resolved,
                baseUrl: baseUrl));
        });

        return app;
    }

    /// <summary>
    /// PROP-15.15: one shape for disabled / null-client / success branches.
    /// </summary>
    public static object BuildModelsPayload(
        IReadOnlyList<string> models,
        bool available,
        string? detail,
        string? hostModel,
        string? personaInferenceModel,
        string resolvedInferenceModel,
        string baseUrl) => new
    {
        models,
        available,
        detail,
        hostModel,
        personaInferenceModel,
        resolvedInferenceModel,
        baseUrl
    };
}
