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

        // Optional: nur wenn du sicherstellen willst, dass die Tabelle existiert.
        // Bei einer fertigen Ressourcen-DB kann man das auch weglassen.
        // await _database.CreateTableAsync<BoardGame>();

        System.Diagnostics.Debug.WriteLine($"SQLite DB Path: {DatabaseConstants.DatabasePath}");
    }

    private static async Task CopyDatabaseToAppDataDirectoryAsync()
    {
        string targetPath = DatabaseConstants.DatabasePath;

        //if (File.Exists(targetPath))
          //  return;

        using Stream inputStream =
            await FileSystem.Current.OpenAppPackageFileAsync(DatabaseConstants.DatabaseFilename);

        using FileStream outputStream = File.Create(targetPath);

        await inputStream.CopyToAsync(outputStream);
    }

    public async Task<List<games>> GetGamesAsync()
    {
        await InitAsync();

        return await _database!
            .Table<games>()
            .OrderBy(game => game.title)
            .ToListAsync();
    }

    public async Task<games?> GetGameAsync(int id)
    {
        await InitAsync();

        return await _database!
            .Table<games>()
            .Where(game => game.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveGameAsync(games game)
    {
        await InitAsync();

        if (game.Id != 0)
            return await _database!.UpdateAsync(game);

        return await _database!.InsertAsync(game);
    }

    public async Task<int> DeleteGameAsync(games game)
    {
        await InitAsync();

        return await _database!.DeleteAsync(game);
    }
}