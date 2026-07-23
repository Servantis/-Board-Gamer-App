namespace BoardGamerApp.Services;

public class DeviceIdentityService
{
    private const string InstallationIdKey = "boardgamer_installation_id";

    public string GetInstallationId()
    {
        var installationId = Preferences.Default.Get(InstallationIdKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(installationId))
            return installationId;

        installationId = Guid.NewGuid().ToString();

        Preferences.Default.Set(InstallationIdKey, installationId);

        return installationId;
    }
}