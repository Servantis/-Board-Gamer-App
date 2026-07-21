using System.Text.Json;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using SQLite;

namespace BoardGamerApp.Repositories;

public class BoardGameRepository
{
    private readonly DatabaseService _databaseService;
    private readonly SyncOutboxService _syncOutboxService;

    public BoardGameRepository(
        DatabaseService databaseService,
        SyncOutboxService syncOutboxService)
    {
        _databaseService = databaseService;
        _syncOutboxService = syncOutboxService;
    }

    public async Task<List<BoardGame>> GetAllAsync()
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<BoardGame>()
            .Where(game => game.DeletedAt == null)
            .OrderBy(game => game.Title)
            .ToListAsync();
    }

    public async Task<List<BoardGame>> GetByGroupAsync(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Die Gruppen-ID darf nicht leer sein.", nameof(groupId));

        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<BoardGame>()
            .Where(game => game.GroupId == groupId && game.DeletedAt == null)
            .OrderBy(game => game.Title)
            .ToListAsync();
    }

    public async Task<BoardGame?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Die Spiel-ID darf nicht leer sein.", nameof(id));

        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<BoardGame>()
            .Where(game => game.Id == id && game.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(BoardGame game)
    {
        ValidateBoardGame(game);

        var database = await _databaseService.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        if (string.IsNullOrWhiteSpace(game.Id))
            game.Id = Guid.NewGuid().ToString();

        game.CreatedAt = now;
        game.UpdatedAt = now;
        game.DeletedAt = null;
        game.Version = 1;

        await database.InsertAsync(game);

        await _syncOutboxService.AddEntityAsync(
            database,
            "games",
            game,
            BoardGamerConstants.SyncOperations.Insert
        );
    }

    public async Task UpdateAsync(BoardGame game)
    {
        ValidateBoardGame(game);

        var database = await _databaseService.GetConnectionAsync();

        game.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        game.Version += 1;

        await database.UpdateAsync(game);

        await _syncOutboxService.AddEntityAsync(
            database,
            "games",
            game,
            BoardGamerConstants.SyncOperations.Update
        );
    }

    public async Task SoftDeleteAsync(BoardGame game)
    {
        var database = await _databaseService.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        game.DeletedAt = now;
        game.UpdatedAt = now;
        game.Version += 1;

        await database.UpdateAsync(game);

        await _syncOutboxService.AddEntityAsync(
            database,
            "games",
            game,
            BoardGamerConstants.SyncOperations.Delete
        );
    }

    public async Task SoftDeleteByIdAsync(string id)
    {
        var game = await GetByIdAsync(id);

        if (game is null)
            return;

        await SoftDeleteAsync(game);
    }

    private static void ValidateBoardGame(BoardGame game)
    {
        if (string.IsNullOrWhiteSpace(game.GroupId))
            throw new InvalidOperationException("Das Spiel muss einer Gruppe zugeordnet sein.");

        if (string.IsNullOrWhiteSpace(game.Title))
            throw new InvalidOperationException("Der Spieltitel darf nicht leer sein.");

        if (game.MinPlayers.HasValue && game.MinPlayers.Value <= 0)
            throw new InvalidOperationException("Die minimale Spieleranzahl muss größer als 0 sein.");

        if (game.MaxPlayers.HasValue && game.MaxPlayers.Value <= 0)
            throw new InvalidOperationException("Die maximale Spieleranzahl muss größer als 0 sein.");

        if (game.MinPlayers.HasValue &&
            game.MaxPlayers.HasValue &&
            game.MaxPlayers.Value < game.MinPlayers.Value)
        {
            throw new InvalidOperationException(
                "Die maximale Spieleranzahl darf nicht kleiner als die minimale Spieleranzahl sein."
            );
        }

        if (game.DurationMinutes.HasValue && game.DurationMinutes.Value <= 0)
            throw new InvalidOperationException("Die Spieldauer muss größer als 0 sein.");
    }

}