using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Repositories;

public interface IGroupMemberRepository
{
    Task<GamingGroup?> GetDefaultGroupAsync();

    /// <summary>Liefert alle Gruppen, in denen ein Spieler aktives Mitglied ist.</summary>
    Task<List<GamingGroup>> GetGroupsForPlayerAsync(string playerId);

    Task<GamingGroup?> GetGroupByIdAsync(string groupId);

    Task<List<GroupMemberListItem>> GetMembersAsync();

    Task<List<GroupMemberListItem>> GetMembersByGroupIdAsync(string groupId);

    Task<List<GroupMember>> GetGroupMembersByGroupIdAsync(string groupId);

    Task<GroupMember?> GetMemberByIdAsync(string memberId);

    Task AddMemberAsync(
        string groupId,
        string playerId,
        string role = "member",
        int? rotationOrder = null);

    Task UpdateMemberAsync(GroupMember member);

    Task SoftDeleteGroupMemberAsync(string groupId, string playerId);
    Task InviteMemberAsync(
    string groupId,
    string playerId,
    string role = "member");

    Task<List<GroupInvitationItem>>
        GetPendingInvitationsAsync(
            string playerId);

}