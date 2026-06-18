using SQLite;

namespace BoardGamerApp.Models;

[Table("group_members")]
public class GroupMember : BaseSyncEntity
{
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