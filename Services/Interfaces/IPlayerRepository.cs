using BoardGamerApp.Models;

namespace BoardGamerApp.Repositories;

public interface IPlayerRepository
{
    Task<List<Player>> GetActivePlayersAsync();

    Task<Player?> GetPlayerByInstallationIdAsync(string installationId);

    Task<Player?> GetByIdAsync(string playerId);


    Task UpdatePlayerProfileAsync(
        string playerId,
        string name,
        string? email);
}