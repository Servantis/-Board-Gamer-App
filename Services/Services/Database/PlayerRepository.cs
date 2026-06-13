using BoardGamerApp.Data;
using BoardGamerApp.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Services.Database
{
    public class PlayerRepository
    {
        private SQLiteAsyncConnection? _database;

        private async Task InitAsync()
        {
            System.Diagnostics.Debug.WriteLine("DATABASE INITIALIZED");

            if (_database != null)
                return;
            await CopyDatabaseToAppDataDirectoryAsync();

            System.Diagnostics.Debug.WriteLine($"DB PATH = {DatabaseConstants.DatabasePath}");

            _database = new SQLiteAsyncConnection(
                DatabaseConstants.DatabasePath,
                DatabaseConstants.Flags);
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

        public async Task<List<GroupMember>> GetPlayersAsync()
        {
            await InitAsync();
            var raw = await _database!.QueryAsync<GroupMember>("SELECT * FROM players");
            foreach (var p in raw)
            {
                System.Diagnostics.Debug.WriteLine($"RAW: {p.Name} - {p.IsNextHost}");
            }
            return await _database!
                .Table<GroupMember>()
                .ToListAsync();
        }

        public async Task<GroupMember?> GetPlayerAsync(int id)
        {
            await InitAsync();

            return await _database!
                .Table<GroupMember>()
                .Where(player => player.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SavePlayerAsync(GroupMember player)
        {
            await InitAsync();

            if (player.Id != 0)
                return await _database!.UpdateAsync(player);

            return await _database!.InsertAsync(player);
        }

        public async Task SavePlayersAsync(IEnumerable<GroupMember> players)
        {
            await InitAsync();

            foreach (var player in players)
            {
                if (player.Id != 0)
                    await _database!.UpdateAsync(player);
                else
                    await _database!.InsertAsync(player);
            }
        }

        public async Task<int> DeletePlayerAsync(int playerId)
        {
            await InitAsync();

            return await _database!
                .Table<GroupMember>()
                .DeleteAsync(p => p.Id == playerId);
        }
    }
}
