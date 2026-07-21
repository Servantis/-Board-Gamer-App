using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoardGamerApp.Services;

public class MessageApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiCredentialService _apiCredentialService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MessageApiClient(
        HttpClient httpClient,
        ApiCredentialService apiCredentialService)
    {
        _httpClient = httpClient;
        _apiCredentialService = apiCredentialService;
    }

    public async Task<DelayMessageResponse> SendDelayMessageAsync(
        DelayMessageRequest request)
    {
        var apiKey = await _apiCredentialService.GetRequiredApiKeyAsync();

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Es wurde kein API-Key für die API konfiguriert.");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/messages/delay"
        );

        httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        httpRequest.Content = JsonContent.Create(
            request,
            options: JsonOptions
        );

        using var response = await _httpClient.SendAsync(httpRequest);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Nachricht konnte nicht gesendet werden. " +
                $"Status: {(int)response.StatusCode} {response.StatusCode}\n{responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<DelayMessageResponse>(
            responseContent,
            JsonOptions
        );

        if (result is null)
            throw new InvalidOperationException("Die Serverantwort konnte nicht gelesen werden.");

        return result;
    }
}