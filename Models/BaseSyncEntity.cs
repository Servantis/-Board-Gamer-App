using SQLite;
using System.Globalization;

namespace BoardGamerApp.Models;

public abstract class BaseSyncEntity
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string CreatedAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    [Indexed]
    public string UpdatedAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    [Indexed]
    public string? DeletedAt { get; set; }

    public int Version { get; set; } = 1;

    [Ignore]
    public bool IsDeleted => !string.IsNullOrWhiteSpace(DeletedAt);
}

public static class DateTimeHelper
{
    public static string UtcNowIsoString()
    {
        return DateTimeOffset.UtcNow.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture
        );
    }

    public static string ToIsoString(DateTimeOffset dateTime)
    {
        return dateTime
            .ToUniversalTime()
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }
}