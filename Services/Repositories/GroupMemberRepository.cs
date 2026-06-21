using BoardGamerApp.Models;
using BoardGamerApp.Services;

namespace BoardGamerApp.Repositories;

public class GroupMemberRepository : IGroupMemberRepository
{
    private readonly DatabaseService _databaseService;

    public GroupMemberRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<GroupMember>> GetMembersAsync()
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<GroupMember>()
            .ToListAsync();
    }

    public async Task<GroupMember?> GetMemberByIdAsync(int id)
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<GroupMember>()
            .Where(member => member.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveMemberAsync(GroupMember member)
    {
        var database = await _databaseService.GetConnectionAsync();

        System.Diagnostics.Debug.WriteLine(
            $"SAVE GROUP MEMBER: {member.Name} NextHost={member.IsNextHost}");

        if (member.Id != 0)
        {
            return await database.UpdateAsync(member);
        }

        return await database.InsertAsync(member);
    }

    public async Task SaveMembersAsync(IEnumerable<GroupMember> members)
    {
        var database = await _databaseService.GetConnectionAsync();

        foreach (var member in members)
        {
            if (member.Id != 0)
            {
                await database.UpdateAsync(member);
            }
            else
            {
                await database.InsertAsync(member);
            }
        }
    }

    public async Task<int> DeleteMemberAsync(int memberId)
    {
        var database = await _databaseService.GetConnectionAsync();

        return await database
            .Table<GroupMember>()
            .DeleteAsync(member => member.Id == memberId);
    }
}