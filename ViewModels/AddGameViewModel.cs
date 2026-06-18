using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardGamerApp.ViewModels;

public partial class AddGameViewModel : ObservableObject
{
    private readonly BoardGameRepository _boardGameRepository;
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private string title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private string minPlayers = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private string maxPlayers = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private string durationMinutes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private string gameGenre = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public string PreviewTitle =>
        string.IsNullOrWhiteSpace(Title)
            ? "Noch kein Titel"
            : Title.Trim();

    public string PreviewDetails
    {
        get
        {
            var playerText = BuildPlayerText();
            var durationText = BuildDurationText();
            var genreText = string.IsNullOrWhiteSpace(GameGenre)
                ? "Kein Genre"
                : GameGenre.Trim();

            return $"{playerText} · {durationText} · {genreText}";
        }
    }

    public AddGameViewModel(
        BoardGameRepository boardGameRepository,
        DatabaseService databaseService)
    {
        _boardGameRepository = boardGameRepository;
        _databaseService = databaseService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Title))
            {
                await Shell.Current.DisplayAlert(
                    "Eingabe fehlt",
                    "Bitte gib einen Namen für das Spiel ein.",
                    "OK");

                return;
            }

            int? minPlayersValue = ParseNullableInt(MinPlayers);
            int? maxPlayersValue = ParseNullableInt(MaxPlayers);
            int? durationValue = ParseNullableInt(DurationMinutes);

            if (minPlayersValue.HasValue && minPlayersValue.Value <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "Ungültige Eingabe",
                    "Die minimale Spieleranzahl muss größer als 0 sein.",
                    "OK");

                return;
            }

            if (maxPlayersValue.HasValue && maxPlayersValue.Value <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "Ungültige Eingabe",
                    "Die maximale Spieleranzahl muss größer als 0 sein.",
                    "OK");

                return;
            }

            if (minPlayersValue.HasValue &&
                maxPlayersValue.HasValue &&
                maxPlayersValue.Value < minPlayersValue.Value)
            {
                await Shell.Current.DisplayAlert(
                    "Ungültige Eingabe",
                    "Die maximale Spieleranzahl darf nicht kleiner als die minimale Spieleranzahl sein.",
                    "OK");

                return;
            }

            if (durationValue.HasValue && durationValue.Value <= 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ungültige Eingabe",
                    "Die Spieldauer muss größer als 0 Minuten sein.",
                    "OK");

                return;
            }

            var group = await GetDefaultGroupAsync();

            if (group is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Keine Gruppe vorhanden",
                    "Es wurde keine Spielgruppe gefunden. Bitte lege zuerst eine Gruppe an.",
                    "OK");

                return;
            }

            var newGame = new BoardGame
            {
                GroupId = group.Id,
                Title = Title.Trim(),
                MinPlayers = minPlayersValue,
                MaxPlayers = maxPlayersValue,
                DurationMinutes = durationValue,
                GameGenre = string.IsNullOrWhiteSpace(GameGenre) ? null : GameGenre.Trim(),
                OwnerPlayerId = null
            };

            await _boardGameRepository.AddAsync(newGame);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Das Spiel konnte nicht gespeichert werden.\n{ex.Message}",
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
        await Shell.Current.GoToAsync("..");
    }

    private async Task<GamingGroup?> GetDefaultGroupAsync()
    {
        var groups = await _databaseService.GetNotDeletedAsync<GamingGroup>();
        return groups.FirstOrDefault();
    }

    private static int? ParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value.Trim(), out int result))
            return result;

        return null;
    }

    private string BuildPlayerText()
    {
        int? min = ParseNullableInt(MinPlayers);
        int? max = ParseNullableInt(MaxPlayers);

        if (min.HasValue && max.HasValue)
            return $"{min.Value} - {max.Value} Spieler";

        if (min.HasValue)
            return $"ab {min.Value} Spieler";

        if (max.HasValue)
            return $"bis {max.Value} Spieler";

        return "Keine Spieleranzahl";
    }

    private string BuildDurationText()
    {
        int? duration = ParseNullableInt(DurationMinutes);

        if (!duration.HasValue)
            return "Keine Dauer";

        if (duration.Value < 60)
            return $"{duration.Value} Min.";

        int hours = duration.Value / 60;
        int minutes = duration.Value % 60;

        if (minutes == 0)
            return $"{hours} Std.";

        return $"{hours} Std. {minutes} Min.";
    }
}