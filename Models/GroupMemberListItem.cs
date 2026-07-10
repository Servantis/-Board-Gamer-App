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

    // UI-Kompatibilität für deine bestehende View
    public string DisplayName => PlayerName;

    public string? Email => PlayerEmail;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                return "?";
            }

            var parts = PlayerName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return parts[0][0].ToString().ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    // Gibt es in der neuen DB aktuell nicht direkt.
    // Bleibt erstmal false, damit der alte Badge nicht angezeigt wird.
    public bool HostedFlag { get; set; }

    // Wird im ViewModel anhand der Rotation gesetzt.
    public bool IsNextHost { get; set; }

    // bool für Löschbutton ander Gruppen-Mitglieder: admins/owner -> angezeigen, member -> keine Button
    [Ignore]
    public bool CanRemove { get; set; }
}