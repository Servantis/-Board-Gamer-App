using SQLite;

namespace BoardGamerApp.Models;

[Table("group_members")]
public class GroupMember : BaseSyncEntity
{
    // Explizite [Column]-Attribute sind hier wichtig: die Tabelle group_members
    // benutzt snake_case-Spaltennamen (group_id, player_id, rotation_order). Ohne
    // [Column] würde SQLite-net beim automatischen Lesen/Schreiben (z. B. über
    // DatabaseService.GetNotDeletedAsync<GroupMember>()) stattdessen die reinen
    // C#-Property-Namen als Spaltennamen erwarten (GroupId, PlayerId,
    // RotationOrder) - das matcht nicht (anders als bei Role/Status, wo sich
    // Property- und Spaltenname nur in der Groß-/Kleinschreibung unterscheiden),
    // und die Werte blieben beim Lesen leer/null.
    [Indexed(Name = "ux_group_members_group_player", Order = 1, Unique = true)]
    [NotNull]
    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Indexed(Name = "ux_group_members_group_player", Order = 2, Unique = true)]
    [NotNull]
    [Column("player_id")]
    public string PlayerId { get; set; } = string.Empty;

    [NotNull]
    [Column("role")]
    public string Role { get; set; } = BoardGamerConstants.GroupRoles.Member;

    [NotNull]
    [Column("status")]
    public string Status { get; set; } = BoardGamerConstants.GroupMemberStatus.Active;

    [Column("rotation_order")]
    public int? RotationOrder { get; set; }
}