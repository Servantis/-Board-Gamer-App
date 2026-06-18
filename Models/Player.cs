using SQLite;

namespace BoardGamerApp.Models;

[Table("players")]
public class Player : BaseSyncEntity
{
    [NotNull]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Indexed(Unique = true)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("is_active")]
    public int IsActive { get; set; } = 1;

    [Ignore]
    public bool Active
    {
        get => IsActive == 1;
        set => IsActive = value ? 1 : 0;
    }
}