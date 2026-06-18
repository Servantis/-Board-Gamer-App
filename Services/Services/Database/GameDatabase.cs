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

    public async Task<List<Game>> GetGamesAsync()
    {
        await InitAsync();

        return await _database!
            .Table<Game>()
            .OrderBy(game => game.Title)
            .ToListAsync();
    }

    public async Task<int> SaveGameAsync(Game game)
    {
        await InitAsync();

        if (game.Id != 0)
            return await _database!.UpdateAsync(game);

        return await _database!.InsertAsync(game);
    }

    public async Task<int> DeleteGameAsync(Game game)
    {
        await InitAsync();

        return await _database!.DeleteAsync(game);
    }
}