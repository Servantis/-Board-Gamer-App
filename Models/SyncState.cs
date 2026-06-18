using SQLite;

namespace BoardGamerApp.Models;

[Table("sync_state")]
public class SyncState
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = "default";

    [Column("last_pull_at")]
    public string? LastPullAt { get; set; }

    [Column("last_push_at")]
    public string? LastPushAt { get; set; }
}