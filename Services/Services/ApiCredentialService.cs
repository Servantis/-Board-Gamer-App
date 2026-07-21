namespace BoardGamerApp.Services;

public class ApiCredentialService
{
    private const string ApiKeyStorageKey = "boardgamer_api_key";

    public async Task<string?> GetApiKeyAsync()
    {
        try
        {
            var apiKey = await SecureStorage.Default.GetAsync(ApiKeyStorageKey);

            return string.IsNullOrWhiteSpace(apiKey)
                ? null
                : apiKey.Trim();
        }
        catch
        {
            SecureStorage.Default.Remove(ApiKeyStorageKey);
            return null;
        }
    }

    public async Task<string> GetRequiredApiKeyAsync()
    {
        var apiKey = await GetApiKeyAsync();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Es ist kein API-Key eingerichtet. Bitte hinterlege den API-Key in den Einstellungen.");
        }

        return apiKey;
    }

    public async Task SaveApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Der API-Key darf nicht leer sein.");

        await SecureStorage.Default.SetAsync(
            ApiKeyStorageKey,
            apiKey.Trim());
    }

    public void ClearApiKey()
    {
        SecureStorage.Default.Remove(ApiKeyStorageKey);
    }
}