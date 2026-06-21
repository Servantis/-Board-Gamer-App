namespace BoardGamerApp.Repositories;

public interface IPlayerDeviceRepository
{
    Task LinkInstallationToPlayerAsync(
        string playerId,
        string installationId,
        string? deviceName,
        string? platform);

    Task UpdateLastSeenAsync(string installationId);
}