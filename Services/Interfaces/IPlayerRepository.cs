using BoardGamerApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Interfaces
{
    public interface IPlayerRepository
    {
        Task<List<GroupMember>> GetPlayersAsync();
        Task<GroupMember?> GetPlayerByIdAsync(int id);
        Task<int> SavePlayerAsync(GroupMember player);
        Task SavePlayersAsync(IEnumerable<GroupMember> players);
        Task<int> DeletePlayerAsync(int playerId);
    }
}
