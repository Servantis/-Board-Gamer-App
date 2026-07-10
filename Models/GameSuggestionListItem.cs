namespace BoardGamerApp.Models;

public class GameSuggestionListItem
{
    public string SuggestionId { get; set; } = string.Empty;

    public string GameNightId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string GameTitle { get; set; } = string.Empty;

    public string? GameGenre { get; set; }

    public int? MinPlayers { get; set; }

    public int? MaxPlayers { get; set; }

    public int? DurationMinutes { get; set; }

    public string SuggestedByPlayerId { get; set; } = string.Empty;

    public string SuggestedByPlayerName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public int VoteCount { get; set; }

    public int HasCurrentPlayerVotedValue { get; set; }

    public bool HasCurrentPlayerVoted => HasCurrentPlayerVotedValue == 1;

    public string PlayerRange
    {
        get
        {
            if (MinPlayers is null && MaxPlayers is null)
            {
                return "-";
            }

            if (MinPlayers == MaxPlayers)
            {
                return $"{MinPlayers}";
            }

            return $"{MinPlayers}-{MaxPlayers}";
        }
    }

    public string DurationText =>
        DurationMinutes is null
            ? "-"
            : $"{DurationMinutes} Min.";
}