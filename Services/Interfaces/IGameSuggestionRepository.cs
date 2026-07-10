using BoardGamerApp.Models;

namespace BoardGamerApp.Repositories;

public interface IGameSuggestionRepository
{
    Task<List<GameSuggestionListItem>> GetSuggestionsForGameNightAsync(
        string gameNightId,
        string currentPlayerId);

    Task AddSuggestionAsync(
        string gameNightId,
        string gameId,
        string suggestedByPlayerId,
        string? comment);

    Task ToggleVoteAsync(
        string suggestionId,
        string playerId);

    Task SoftDeleteSuggestionAsync(
        string suggestionId);
}