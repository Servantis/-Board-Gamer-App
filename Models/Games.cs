using SQLite;
using System.ComponentModel.DataAnnotations.Schema;
using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;

namespace BoardGamerApp.Models;

[Table("games")]
public class games
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string title { get; set; } = string.Empty;

    public string game_genre { get; set; } = string.Empty;

    public int min_players { get; set; }

    public int max_players { get; set; }

    public int duration_minutes { get; set; }

    public int owner_player_id { get; set; }



    [Ignore]
    public string PlayerRange =>
        min_players == max_players
            ? $"{min_players}"
            : $"{min_players}–{max_players}";

    [Ignore]
    public string DurationText => $"{duration_minutes} Min.";
}