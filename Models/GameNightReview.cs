using SQLite;

namespace BoardGamerApp.Models;

[Table("game_night_reviews")]
public class GameNightReview : BaseSyncEntity
{
    [Indexed(Name = "ux_game_night_reviews_night_reviewer", Order = 1, Unique = true)]
    [NotNull]
    [Column("game_night_id")]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_night_reviews_night_reviewer", Order = 2, Unique = true)]
    [NotNull]
    [Column("reviewer_player_id")]
    public string ReviewerPlayerId { get; set; } = string.Empty;

    [Indexed]
    [Column("reviewed_host_player_id")]
    public string? ReviewedHostPlayerId { get; set; }

    [Column("host_rating")]
    public int? HostRating { get; set; }

    [Column("food_rating")]
    public int? FoodRating { get; set; }

    [Column("overall_rating")]
    public int OverallRating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }
}