using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoulCore.Core.Persona;
using SoulCore.Core.SharedSpaces;

namespace SoulCore.Host.SharedSpaces;

public static class SharedSpaceApiEndpoints
{
    public static IEndpointRouteBuilder MapSharedSpacesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shared-spaces");

        group.MapGet("/", async (ISharedSpaceStore store, CancellationToken ct) =>
        {
            var list = await store.ListAsync(ct).ConfigureAwait(false);
            return Results.Json(new { spaces = list });
        });

        group.MapGet("/{spaceId}", async (string spaceId, ISharedSpaceStore store, CancellationToken ct) =>
        {
            var space = await store.GetAsync(spaceId, ct).ConfigureAwait(false);
            return space is null ? Results.NotFound() : Results.Json(space);
        });

        group.MapPost("/", async (HttpRequest request, ISharedSpaceStore store, CancellationToken ct) =>
        {
            SharedSpace? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<SharedSpace>(
                    request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    ct).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            if (body is null || string.IsNullOrWhiteSpace(body.SpaceId) || string.IsNullOrWhiteSpace(body.DisplayName))
                return Results.BadRequest(new { error = "spaceId and displayName required" });

            try
            {
                var saved = await store.UpsertAsync(body, ct).ConfigureAwait(false);
                return Results.Json(saved);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{spaceId}", async (string spaceId, ISharedSpaceStore store, CancellationToken ct) =>
        {
            var ok = await store.DeleteAsync(spaceId, ct).ConfigureAwait(false);
            return ok ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });

        group.MapGet("/{spaceId}/facts", async (string spaceId, ISharedSpaceStore store, CancellationToken ct) =>
        {
            if (await store.GetAsync(spaceId, ct).ConfigureAwait(false) is null)
                return Results.NotFound();
            using var mem = store.OpenMemory(spaceId);
            var facts = await mem.RecallRecentAsync(50, ct).ConfigureAwait(false);
            return Results.Json(new { spaceId, facts });
        });

        group.MapPost("/{spaceId}/facts", async (string spaceId, HttpRequest request, ISharedSpaceStore store, CancellationToken ct) =>
        {
            if (await store.GetAsync(spaceId, ct).ConfigureAwait(false) is null)
                return Results.NotFound();

            using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("text", out var textEl)
                || textEl.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(textEl.GetString()))
                return Results.BadRequest(new { error = "text required" });

            var source = "operator";
            if (doc.RootElement.TryGetProperty("source", out var srcEl)
                && srcEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(srcEl.GetString()))
                source = srcEl.GetString()!.Trim();

            using var mem = store.OpenMemory(spaceId);
            var id = await mem.WriteFactAsync(textEl.GetString()!, source, ct).ConfigureAwait(false);
            return Results.Json(new { id, spaceId });
        });

        // Attach / detach on a persona pack (updates SharedSpaceIds).
        group.MapPost("/attach", async (
            HttpRequest request,
            IPersonaPackStore packs,
            ISharedSpaceStore spaces,
            IPersonaSession session,
            CancellationToken ct) =>
        {
            using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("personaId", out var pEl)
                || !doc.RootElement.TryGetProperty("spaceId", out var sEl))
                return Results.BadRequest(new { error = "personaId and spaceId required" });

            var personaId = pEl.GetString();
            var spaceId = sEl.GetString();
            if (string.IsNullOrWhiteSpace(personaId) || string.IsNullOrWhiteSpace(spaceId))
                return Results.BadRequest(new { error = "personaId and spaceId required" });

            var pack = await packs.GetAsync(personaId, ct).ConfigureAwait(false);
            if (pack is null)
                return Results.NotFound(new { error = "persona not found" });
            if (await spaces.GetAsync(spaceId, ct).ConfigureAwait(false) is null)
                return Results.NotFound(new { error = "space not found" });

            var set = new HashSet<string>(pack.SharedSpaceIds ?? [], StringComparer.OrdinalIgnoreCase)
            {
                spaceId.Trim().ToLowerInvariant()
            };
            pack.SharedSpaceIds = set.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            pack.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var saved = await packs.UpsertAsync(pack, ct).ConfigureAwait(false);
            if (string.Equals(session.ActivePersonaId, saved.PersonaId, StringComparison.OrdinalIgnoreCase))
                await session.ReloadActiveAsync(ct).ConfigureAwait(false);
            return Results.Json(saved);
        });

        group.MapPost("/detach", async (
            HttpRequest request,
            IPersonaPackStore packs,
            IPersonaSession session,
            CancellationToken ct) =>
        {
            using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("personaId", out var pEl)
                || !doc.RootElement.TryGetProperty("spaceId", out var sEl))
                return Results.BadRequest(new { error = "personaId and spaceId required" });

            var personaId = pEl.GetString();
            var spaceId = sEl.GetString();
            if (string.IsNullOrWhiteSpace(personaId) || string.IsNullOrWhiteSpace(spaceId))
                return Results.BadRequest(new { error = "personaId and spaceId required" });

            var pack = await packs.GetAsync(personaId, ct).ConfigureAwait(false);
            if (pack is null)
                return Results.NotFound(new { error = "persona not found" });

            pack.SharedSpaceIds = (pack.SharedSpaceIds ?? [])
                .Where(id => !string.Equals(id, spaceId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            pack.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var saved = await packs.UpsertAsync(pack, ct).ConfigureAwait(false);
            if (string.Equals(session.ActivePersonaId, saved.PersonaId, StringComparison.OrdinalIgnoreCase))
                await session.ReloadActiveAsync(ct).ConfigureAwait(false);
            return Results.Json(saved);
        });

        return app;
    }
}
