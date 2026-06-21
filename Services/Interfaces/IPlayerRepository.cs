using BoardGamerApp.Models;

namespace BoardGamerApp.Repositories;

public interface IPlayerRepository
{
    Task<List<Player>> GetActivePlayersAsync();

    Task<Player?> GetPlayerByInstallationIdAsync(string installationId);
}