using SQLite;

namespace BoardGamerApp.Models;

[Table("game_votes")]
public class GameVote : BaseSyncEntity
{
    [Indexed(Name = "ux_game_votes_suggestion_player", Order = 1, Unique = true)]
    [NotNull]
    [Column("suggestion_id")]
    public string SuggestionId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_votes_suggestion_player", Order = 2, Unique = true)]
    [NotNull]
    [Column("player_id")]
    public string PlayerId { get; set; } = string.Empty;

    [Column("vote_value")]
    public int VoteValue { get; set; } = 1;
}