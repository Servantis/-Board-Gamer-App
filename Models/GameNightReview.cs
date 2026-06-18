using SQLite;

namespace BoardGamerApp.Models;

[Table("game_night_reviews")]
public class GameNightReview : BaseSyncEntity
{
    [Indexed(Name = "ux_game_night_reviews_night_reviewer", Order = 1, Unique = true)]
    [NotNull]
    public string GameNightId { get; set; } = string.Empty;

    [Indexed(Name = "ux_game_night_reviews_night_reviewer", Order = 2, Unique = true)]
    [NotNull]
    public string ReviewerPlayerId { get; set; } = string.Empty;

    [Indexed]
    public string? ReviewedHostPlayerId { get; set; }

    public int? HostRating { get; set; }

    public int? FoodRating { get; set; }

    public int OverallRating { get; set; }

    public string? Comment { get; set; }
}