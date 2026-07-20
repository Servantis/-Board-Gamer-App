using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoardGamerApp.Services;

public class SyncApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SyncApiOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SyncApiClient(HttpClient httpClient, SyncApiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }
    //Push
    public async Task<SyncPushResponse> PushAsync(SyncPushRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Es wurde kein API-Key für die Sync-API konfiguriert.");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/sync/push"
        );

        httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);

        httpRequest.Content = JsonContent.Create(
            request,
            options: JsonOptions
        );

        using var response = await _httpClient.SendAsync(httpRequest);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sync-Push fehlgeschlagen. Status: {(int)response.StatusCode} {response.StatusCode}\n{responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<SyncPushResponse>(
            responseContent,
            JsonOptions
        );

        if (result is null)
            throw new InvalidOperationException("Die Sync-Antwort konnte nicht gelesen werden.");

        return result;
    }
    //Pull
    public async Task<SyncPullResponse> PullAsync(string? since)
    {
        var apiKey = _options.ApiKey?.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Es wurde kein API-Key für die Sync-API konfiguriert.");

        var relativeUrl = string.IsNullOrWhiteSpace(since)
            ? "api/sync/pull"
            : $"api/sync/pull?since={Uri.EscapeDataString(since)}";

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            relativeUrl
        );

        httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sync-Pull fehlgeschlagen. Status: {(int)response.StatusCode} {response.StatusCode}\n{responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<SyncPullResponse>(
            responseContent,
            JsonOptions
        );

        if (result is null)
            throw new InvalidOperationException("Die Sync-Pull-Antwort konnte nicht gelesen werden.");

        return result;
    }

}