using Microsoft.Maui.Storage;

namespace BoardGamerApp.Services;

public class InstallationService
{
    private const string InstallationIdKey = "installation_id";

    public async Task<string> GetOrCreateInstallationIdAsync()
    {
        var installationId = await SecureStorage.Default.GetAsync(InstallationIdKey);

        if (!string.IsNullOrWhiteSpace(installationId))
        {
            return installationId;
        }

        installationId = Guid.NewGuid().ToString();

        await SecureStorage.Default.SetAsync(InstallationIdKey, installationId);

        return installationId;
    }

    public void ResetInstallationId()
    {
        SecureStorage.Default.Remove(InstallationIdKey);
    }
}