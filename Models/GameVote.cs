using SQLite;

namespace BoardGamerApp.Models;

[Table("game_votes")]
public class GameVote : BaseSyncEntity
{
    [Indexed(Name = "ux_game_votes_suggestion_player", Order = 1, Unique = true)]
    [NotNull]
    public string SuggestionId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_votes_suggestion_player", Order = 2, Unique = true)]
    [NotNull]
    public string PlayerId { get; set; } = string.Empty;

    public int VoteValue { get; set; } = 1;
}