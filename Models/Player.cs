using SQLite;

namespace BoardGamerApp.Models;

[Table("players")]
public class Player : BaseSyncEntity
{
    [NotNull]
    public string Name { get; set; } = string.Empty;

    [Indexed(Unique = true)]
    public string? Email { get; set; }

    public int IsActive { get; set; } = 1;

    [Ignore]
    public bool Active
    {
        get => IsActive == 1;
        set => IsActive = value ? 1 : 0;
    }
}