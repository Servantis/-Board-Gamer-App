using BoardGamerApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<List<GroupMember>> GetPlayersAsync();
        Task<GroupMember> GetPlayerByIdAsync(int id);
        Task SavePlayerAsync(GroupMember player);
        Task SavePlayersAsync(IEnumerable<GroupMember> players);
        Task DeletePlayerAsync(int playerId);
    }
}
