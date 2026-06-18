using BoardGamerApp.Models;
using SQLite;

namespace BoardGamerApp.Data;

public class GameDatabase
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        await CopyDatabaseToAppDataDirectoryAsync();

        _database = new SQLiteAsyncConnection(
            DatabaseConstants.DatabasePath,
            DatabaseConstants.Flags);

        // Bei vorbereiteter Ressourcen-DB optional.
        // await _database.CreateTableAsync<Game>();
    }

    private static async Task CopyDatabaseToAppDataDirectoryAsync()
    {
        string targetPath = DatabaseConstants.DatabasePath;

        if (File.Exists(targetPath))
            return;

        using Stream inputStream =
            await FileSystem.Current.OpenAppPackageFileAsync(DatabaseConstants.DatabaseFilename);

        using FileStream outputStream = File.Create(targetPath);

        await inputStream.CopyToAsync(outputStream);
    }

    public async Task<List<BoardGame>> GetGamesAsync()
    {
        await InitAsync();

        return await _database!
            .Table<BoardGame>()
            .OrderBy(game => game.Title)
            .ToListAsync();
    }

    public async Task<int> SaveGameAsync(BoardGame game)
    {
        await InitAsync();

        if (game.Id is null)
            return await _database!.UpdateAsync(game);

        return await _database!.InsertAsync(game);
    }

    public async Task<int> DeleteGameAsync(BoardGame game)
    {
        await InitAsync();

        return await _database!.DeleteAsync(game);
    }
}