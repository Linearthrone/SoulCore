using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace House.ChatDesktop.Services;

/// <summary>PROP-20.3 — thin HTTP wrappers over Host <c>/api/shared-spaces</c>.</summary>
public sealed partial class SoulCorePersonaClient
{
    public static Uri SharedSpacesUri =>
        new($"http://{ConnectionDefaults.Host}:{ConnectionDefaults.Port}/api/shared-spaces");

    public static Uri SharedSpaceAttachUri => new($"{SharedSpacesUri}/attach");

    public static Uri SharedSpaceDetachUri => new($"{SharedSpacesUri}/detach");

    /// <summary>JSON body for attach / detach.</summary>
    public static string SharedSpaceLinkJson(string personaId, string spaceId) =>
        JsonSerializer.Serialize(new { personaId, spaceId });

    /// <summary>JSON body for creating a space.</summary>
    public static string SharedSpaceCreateJson(string spaceId, string displayName, string? description) =>
        JsonSerializer.Serialize(new { spaceId, displayName, description });

    public async Task<SharedSpaceListSnapshot> ListSharedSpacesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!ConnectionDefaults.IsLocalLoopback(ConnectionDefaults.Host))
        {
            return new SharedSpaceListSnapshot
            {
                Reachable = false,
                Detail = $"Non-loopback host blocked: {ConnectionDefaults.Host}"
            };
        }

        try
        {
            using var response = await _http.GetAsync(SharedSpacesUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new SharedSpaceListSnapshot
                {
                    Reachable = true,
                    Ok = false,
                    HttpStatus = (int)response.StatusCode,
                    Detail = response.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? "Host has no shared-spaces API (/api/shared-spaces)."
                        : $"HTTP {(int)response.StatusCode}"
                };
            }

            var dto = await response.Content.ReadFromJsonAsync<SharedSpaceListDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var spaces = (dto?.Spaces ?? new List<SharedSpaceDto>())
                .Where(s => !string.IsNullOrWhiteSpace(s.SpaceId))
                .Select(ToSharedSpaceInfo)
                .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new SharedSpaceListSnapshot { Reachable = true, Ok = true, Spaces = spaces };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new SharedSpaceListSnapshot { Reachable = false, Detail = ex.Message };
        }
    }

    public async Task<SharedSpaceWriteSnapshot> CreateSharedSpaceAsync(
        string spaceId,
        string displayName,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (!ConnectionDefaults.IsLocalLoopback(ConnectionDefaults.Host))
        {
            return new SharedSpaceWriteSnapshot
            {
                Reachable = false,
                Detail = $"Non-loopback host blocked: {ConnectionDefaults.Host}"
            };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, SharedSpacesUri)
            {
                Content = new StringContent(
                    SharedSpaceCreateJson(spaceId, displayName, description),
                    Encoding.UTF8,
                    "application/json")
            };
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new SharedSpaceWriteSnapshot
                {
                    Reachable = true,
                    Ok = false,
                    Detail = string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : TrimDetail(body)
                };
            }

            var dto = await response.Content.ReadFromJsonAsync<SharedSpaceDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return new SharedSpaceWriteSnapshot
            {
                Reachable = true,
                Ok = dto is not null,
                Space = dto is null ? null : ToSharedSpaceInfo(dto),
                Detail = dto is null ? "Empty response" : null
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new SharedSpaceWriteSnapshot { Reachable = false, Detail = ex.Message };
        }
    }

    public Task<SharedSpaceWriteSnapshot> AttachSharedSpaceAsync(
        string personaId,
        string spaceId,
        CancellationToken cancellationToken = default) =>
        LinkSharedSpaceAsync(SharedSpaceAttachUri, personaId, spaceId, cancellationToken);

    public Task<SharedSpaceWriteSnapshot> DetachSharedSpaceAsync(
        string personaId,
        string spaceId,
        CancellationToken cancellationToken = default) =>
        LinkSharedSpaceAsync(SharedSpaceDetachUri, personaId, spaceId, cancellationToken);

    private async Task<SharedSpaceWriteSnapshot> LinkSharedSpaceAsync(
        Uri uri,
        string personaId,
        string spaceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(personaId) || string.IsNullOrWhiteSpace(spaceId))
            return new SharedSpaceWriteSnapshot { Reachable = true, Ok = false, Detail = "personaId and spaceId required" };

        if (!ConnectionDefaults.IsLocalLoopback(ConnectionDefaults.Host))
        {
            return new SharedSpaceWriteSnapshot
            {
                Reachable = false,
                Detail = $"Non-loopback host blocked: {ConnectionDefaults.Host}"
            };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(
                    SharedSpaceLinkJson(personaId.Trim(), spaceId.Trim()),
                    Encoding.UTF8,
                    "application/json")
            };
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new SharedSpaceWriteSnapshot
                {
                    Reachable = true,
                    Ok = false,
                    Detail = string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : TrimDetail(body)
                };
            }

            return new SharedSpaceWriteSnapshot { Reachable = true, Ok = true };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new SharedSpaceWriteSnapshot { Reachable = false, Detail = ex.Message };
        }
    }

    private static SharedSpaceInfo ToSharedSpaceInfo(SharedSpaceDto dto) => new()
    {
        SpaceId = (dto.SpaceId ?? "").Trim(),
        DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? (dto.SpaceId ?? "").Trim() : dto.DisplayName.Trim(),
        Description = dto.Description
    };

    private sealed class SharedSpaceListDto
    {
        [JsonPropertyName("spaces")]
        public List<SharedSpaceDto>? Spaces { get; set; }
    }

    private sealed class SharedSpaceDto
    {
        [JsonPropertyName("spaceId")]
        public string? SpaceId { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}

public sealed class SharedSpaceInfo
{
    public string SpaceId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Description { get; init; }

    public override string ToString() => DisplayName;
}

public sealed class SharedSpaceListSnapshot
{
    public bool Reachable { get; init; }
    public bool Ok { get; init; }
    public int HttpStatus { get; init; }
    public string? Detail { get; init; }
    public IReadOnlyList<SharedSpaceInfo> Spaces { get; init; } = Array.Empty<SharedSpaceInfo>();
}

public sealed class SharedSpaceWriteSnapshot
{
    public bool Reachable { get; init; }
    public bool Ok { get; init; }
    public string? Detail { get; init; }
    public SharedSpaceInfo? Space { get; init; }
}
