using BoardGamerApp.Models;

namespace BoardGamerApp.Repositories;

public interface IGroupMemberRepository
{
    Task<List<GroupMember>> GetMembersAsync();

    Task<GroupMember?> GetMemberByIdAsync(int id);

    Task<int> SaveMemberAsync(GroupMember member);

    Task SaveMembersAsync(IEnumerable<GroupMember> members);

    Task<int> DeleteMemberAsync(int memberId);
}