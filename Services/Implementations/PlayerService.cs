using BoardGamerApp.Data;
using BoardGamerApp.Models;
using BoardGamerApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Implementations
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _repository;

        public PlayerService(IPlayerRepository repository)
        {
            _repository = repository;
        }

        public Task<List<GroupMember>> GetPlayersAsync()
        {
            return _repository.GetPlayersAsync();
        }

        public Task<GroupMember> GetPlayerByIdAsync(int playerId)
        {
            return _repository.GetPlayerByIdAsync(playerId);
        }

        public async Task SavePlayerAsync(GroupMember player)
        {
            await _repository.SavePlayerAsync(player);
        }

        public async Task SavePlayersAsync(IEnumerable<GroupMember> players)
        {
            await _repository.SavePlayersAsync(players);
        }

        public async Task DeletePlayerAsync(int playerId)
        {
            await _repository.DeletePlayerAsync(playerId);
        }
    }
}
