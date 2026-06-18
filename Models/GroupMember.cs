using SQLite;

namespace BoardGamerApp.Models;

[Table("group_members")]
public class GroupMember : BaseSyncEntity
{
    [Indexed(Name = "ux_group_members_group_player", Order = 1, Unique = true)]
    [NotNull]
    public string GroupId { get; set; } = string.Empty;

    [Indexed(Name = "ux_group_members_group_player", Order = 2, Unique = true)]
    [NotNull]
    public string PlayerId { get; set; } = string.Empty;

    [NotNull]
    public string Role { get; set; } = BoardGamerConstants.GroupRoles.Member;

    [NotNull]
    public string Status { get; set; } = BoardGamerConstants.GroupMemberStatus.Active;

    public int? RotationOrder { get; set; }
}