using SQLite;

namespace BoardGamerApp.Models;

[Table("sync_outbox")]
public class SyncOutboxEntry
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    [NotNull]
    public string EntityName { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    public string EntityId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    public string Operation { get; set; } = BoardGamerConstants.SyncOperations.Insert;

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    public string CreatedAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    public int RetryCount { get; set; } = 0;

    public string? LastError { get; set; }
}