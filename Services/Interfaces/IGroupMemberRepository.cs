using BoardGamerApp.Models;

namespace BoardGamerApp.Repositories;

public interface IGroupMemberRepository
{
    Task<GamingGroup?> GetDefaultGroupAsync();

    /// <summary>Liefert alle Gruppen, in denen ein Spieler aktives Mitglied ist.</summary>
    Task<List<GamingGroup>> GetGroupsForPlayerAsync(string playerId);

    Task<List<GroupMemberListItem>> GetMembersAsync();

    Task<List<GroupMemberListItem>> GetMembersByGroupIdAsync(string groupId);

    Task<GroupMember?> GetMemberByIdAsync(string memberId);

    Task AddMemberAsync(
        string groupId,
        string playerId,
        string role = "member",
        int? rotationOrder = null);

    Task UpdateMemberAsync(GroupMember member);

    Task UpdateMemberStatusAsync(string memberId, string status);

    Task UpdateRotationOrderAsync(string memberId, int? rotationOrder);

    Task SoftDeleteMemberAsync(string memberId);
}