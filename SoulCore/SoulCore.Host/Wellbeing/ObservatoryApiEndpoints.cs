using System.Text.Json;

namespace SoulCore.Host.Wellbeing;

public static class ObservatoryApiEndpoints
{
    public static IEndpointRouteBuilder MapObservatoryApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/observatory");

        group.MapGet("/stimuli", async (
            IObservatoryStore store,
            int? limit,
            string? kind,
            CancellationToken ct) =>
        {
            var list = await store.ListStimuliAsync(limit ?? 50, kind, ct).ConfigureAwait(false);
            return Results.Json(new { stimuli = list });
        });

        group.MapGet("/stimuli/{stimulusId}", async (
            string stimulusId,
            IObservatoryStore store,
            CancellationToken ct) =>
        {
            var detail = await store.GetAsync(stimulusId, ct).ConfigureAwait(false);
            return detail is null ? Results.NotFound(new { error = "stimulus not found" }) : Results.Json(detail);
        });

        group.MapPost("/stimuli/{stimulusId}/annotate", async (
            string stimulusId,
            HttpRequest request,
            IObservatoryStore store,
            CancellationToken ct) =>
        {
            AnnotateBody? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<AnnotateBody>(
                    request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    ct).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            if (body is null || string.IsNullOrWhiteSpace(body.Note))
                return Results.BadRequest(new { error = "note required" });

            try
            {
                await store.AnnotateAsync(stimulusId, body.Note, body.PhenomenonId, ct).ConfigureAwait(false);
                var detail = await store.GetAsync(stimulusId, ct).ConfigureAwait(false);
                return Results.Json(detail);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        group.MapGet("/settling", (ISoftSwitchService soft) =>
        {
            var snap = soft.GetSettlingSnapshot();
            return Results.Json(new
            {
                settling = snap is not null,
                snapshot = snap
            });
        });

        return app;
    }

    private sealed class AnnotateBody
    {
        public string? Note { get; set; }
        public string? PhenomenonId { get; set; }
    }
}
