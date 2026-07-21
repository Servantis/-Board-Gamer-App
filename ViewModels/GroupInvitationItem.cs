using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.ViewModels;

public class GroupInvitationItem
{
    public string MemberId { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string InviterName { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;
}