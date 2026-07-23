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

    Task<Player> CreatePlayerAsync(string name, string? email);

    Task<List<Player>> SearchAvailablePlayersAsync(
    string groupId,
    string searchText);
}