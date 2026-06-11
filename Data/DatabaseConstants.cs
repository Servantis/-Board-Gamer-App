using SQLite;

namespace BoardGamerApp.Data;

public static class DatabaseConstants
{
    public const string DatabaseFilename = "SQLite.db";

    public const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    public static string DatabasePath =>
         Path.Combine(FileSystem.Current.AppDataDirectory, DatabaseFilename);
}