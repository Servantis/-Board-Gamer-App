using SQLite;

namespace BoardGamerApp.Models;

[Table("player_devices")]
public class PlayerDevice : BaseSyncEntity
{
    [NotNull]
    [Column("player_id")]
    public string PlayerId { get; set; } = string.Empty;

    [NotNull]
    [Indexed(Unique = true)]
    [Column("installation_id")]
    public string InstallationId { get; set; } = string.Empty;

    [Column("device_name")]
    public string? DeviceName { get; set; }

    [Column("platform")]
    public string? Platform { get; set; }

    [Column("last_seen_at")]
    public string? LastSeenAt { get; set; }

    [Column("is_active")]
    public int IsActive { get; set; } = 1;

    [Ignore]
    public bool Active
    {
        get => IsActive == 1;
        set => IsActive = value ? 1 : 0;
    }
}