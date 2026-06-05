using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace BoardGamerApp.Models;

[Table("group_members")]
public class GroupMember : BaseSyncEntity
{
    [Indexed(Name = "ux_group_members_group_player", Order = 1, Unique = true)]
    [NotNull]
    public string GroupId { get; set; } = string.Empty;
    public class GroupMember
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

    [Indexed(Name = "ux_group_members_group_player", Order = 2, Unique = true)]
    [NotNull]
    public string PlayerId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

    [NotNull]
    public string Role { get; set; } = BoardGamerConstants.GroupRoles.Member;
        public bool HostedFlag { get; set; }

    [NotNull]
    public string Status { get; set; } = BoardGamerConstants.GroupMemberStatus.Active;
        public DateTime LastHostedDate { get; set; }
        public string LastHostedDateFormatted => 
            LastHostedDate.ToString("dd.MM.yy");
        public string LastHostedDisplay =>
            $"zuletzt: {LastHostedDateFormatted}";

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;

                var initial = !string.IsNullOrWhiteSpace(LastName)
                    ? $"{LastName[0]}."
                    : "";

                return $"{Name} {initial}".Trim();
            }
        }
    }
}
