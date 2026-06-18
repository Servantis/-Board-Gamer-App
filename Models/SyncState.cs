using SQLite;

namespace BoardGamerApp.Models;

[Table("sync_state")]
public class SyncState
{
    [PrimaryKey]
    public string Id { get; set; } = "default";

    public string? LastPullAt { get; set; }

    public string? LastPushAt { get; set; }
}