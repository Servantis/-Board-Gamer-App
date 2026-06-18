using BoardGamerApp.Data;
using BoardGamerApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardGamerApp.ViewModels;

public partial class AddGameViewModel : ObservableObject
{
    private readonly GameDatabase _database;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string gameGenre = string.Empty;

    [ObservableProperty]
    private string minPlayersText = string.Empty;

    [ObservableProperty]
    private string maxPlayersText = string.Empty;

    [ObservableProperty]
    private string durationMinutesText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public AddGameViewModel(GameDatabase database)
    {
        _database = database;
    }

    [RelayCommand]
    private async Task SaveGameAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Bitte gib einen Spielnamen ein.";
                return;
            }

            if (!int.TryParse(MinPlayersText, out int minPlayers))
            {
                ErrorMessage = "Bitte gib eine gültige minimale Spieleranzahl ein.";
                return;
            }

            if (!int.TryParse(MaxPlayersText, out int maxPlayers))
            {
                ErrorMessage = "Bitte gib eine gültige maximale Spieleranzahl ein.";
                return;
            }

            if (!int.TryParse(DurationMinutesText, out int durationMinutes))
            {
                ErrorMessage = "Bitte gib eine gültige Spieldauer ein.";
                return;
            }

            if (minPlayers <= 0 || maxPlayers <= 0)
            {
                ErrorMessage = "Die Spieleranzahl muss größer als 0 sein.";
                return;
            }

            if (minPlayers > maxPlayers)
            {
                ErrorMessage = "Die minimale Spieleranzahl darf nicht größer als die maximale Spieleranzahl sein.";
                return;
            }

            Game newGame = new()
            {
                Title = Title.Trim(),
                GameGenre = GameGenre.Trim(),
                MinPlayers = minPlayers,
                MaxPlayers = maxPlayers,
                DurationMinutes = durationMinutes,

                // Wichtig:
                // Falls owner_player_id in deiner Datenbank Pflicht ist,
                // muss hier eine gültige Player-ID rein.
                OwnerPlayerId = 1
            };

            await _database.SaveGameAsync(newGame);

            await Shell.Current.GoToAsync("//games");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Spiel konnte nicht gespeichert werden: {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Fehler beim Speichern",
                ex.ToString(),
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("//games");
    }
}