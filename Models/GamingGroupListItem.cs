using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Models;

public class GamingGroupListItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CreatedByPlayerId { get; set; } = string.Empty;

    public string CreatedByPlayerName { get; set; } = string.Empty;

    public bool CanDelete { get; set; }

    public bool CanLeave => !CanDelete;
}
