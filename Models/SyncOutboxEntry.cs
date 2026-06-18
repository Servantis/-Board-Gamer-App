using SQLite;

namespace BoardGamerApp.Models;

[Table("sync_outbox")]
public class SyncOutboxEntry
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    [NotNull]
    [Column("entity_name")]
    public string EntityName { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    [Column("operation")]
    public string Operation { get; set; } = BoardGamerConstants.SyncOperations.Insert;

    [NotNull]
    [Column("payload_json")]
    public string PayloadJson { get; set; } = string.Empty;

    [Indexed]
    [NotNull]
    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("last_error")]
    public string? LastError { get; set; }
}