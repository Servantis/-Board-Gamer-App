using SQLite;

namespace BoardGamerApp.Models;

public class GroupMemberListItem
{
    public string Id { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public string PlayerName { get; set; } = string.Empty;

    public string? PlayerEmail { get; set; }

    public string Role { get; set; } = "member";

    public string Status { get; set; } = "active";

    public int? RotationOrder { get; set; }

    public string GroupName { get; set; } = string.Empty;
    
    [NotNull]
    [Column("hosted_flag")]
    public bool HostedFlag { get; set; } = false;

    [NotNull]
    [Column("is_next_host")]
    public bool IsNextHost { get; set; } = false;

    // UI-Kompatibilität für die bestehende View
    public string DisplayName => PlayerName;

    public string? Email => PlayerEmail;

    // Nur den ersten Buchstaben des Vornamens anzeigen
    [Ignore]
    public string Initials =>
       string.IsNullOrWhiteSpace(PlayerName)
           ? "?"
           : PlayerName.Trim()[0]
               .ToString()
               .ToUpperInvariant();

    [Ignore]
    public bool CanRemove { get; set; }
}