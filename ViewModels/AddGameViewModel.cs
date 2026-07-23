using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

[QueryProperty(nameof(preselectedGroupId), "groupId")]
public partial class AddGameViewModel : ObservableObject
{
    private readonly BoardGameRepository _boardGameRepository;
    private readonly GroupOverviewRepository _groupOverviewRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    public ObservableCollection<GamingGroupListItem> Groups { get; } = new();

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
    [NotifyPropertyChangedFor(nameof(PreviewDetails))]
    private GamingGroupListItem? selectedGroup;

    [ObservableProperty]
    private string? preselectedGroupId;

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

            var groupText = selectedGroup is null
                ? "Keine Gruppe ausgewählt"
                : $"Gruppe: {selectedGroup.Name}";

            return $"{playerText} · {durationText} · {genreText} · {groupText}";
        }
    }

    public AddGameViewModel(
        BoardGameRepository boardGameRepository,
        GroupOverviewRepository groupOverviewRepository,
        CurrentPlayerService currentPlayerService)
    {
        _boardGameRepository = boardGameRepository;
        _groupOverviewRepository = groupOverviewRepository;
        _currentPlayerService = currentPlayerService;
    }

    [RelayCommand]
    public async Task LoadGroupsAsync()
    {
        if (Groups.Count > 0)
            return;

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return;

        var groups = await _groupOverviewRepository.GetGroupsByPlayerIdAsync(currentPlayerId);

        Groups.Clear();

        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        if (!string.IsNullOrWhiteSpace(preselectedGroupId))
        {
            selectedGroup = Groups.FirstOrDefault(group => group.Id == preselectedGroupId);
        }

        selectedGroup ??= Groups.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            if (Groups.Count == 0)
            {
                await LoadGroupsAsync();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Eingabe fehlt",
                    "Bitte gib einen Namen für das Spiel ein.",
                    "OK");

                return;
            }

            if (selectedGroup is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Keine Gruppe ausgewählt",
                    "Bitte wähle aus, mit welcher Gruppe das Spiel geteilt werden soll.",
                    "OK");

                return;
            }

            int? minPlayersValue = ParseNullableInt(MinPlayers);
            int? maxPlayersValue = ParseNullableInt(MaxPlayers);
            int? durationValue = ParseNullableInt(DurationMinutes);

            if (minPlayersValue.HasValue && minPlayersValue.Value <= 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ungültige Eingabe",
                    "Die minimale Spieleranzahl muss größer als 0 sein.",
                    "OK");

                return;
            }

            if (maxPlayersValue.HasValue && maxPlayersValue.Value <= 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ungültige Eingabe",
                    "Die maximale Spieleranzahl muss größer als 0 sein.",
                    "OK");

                return;
            }

            if (minPlayersValue.HasValue &&
                maxPlayersValue.HasValue &&
                maxPlayersValue.Value < minPlayersValue.Value)
            {
                await Shell.Current.DisplayAlertAsync(
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

            var currentPlayerId = _currentPlayerService.PlayerId;

            var newGame = new BoardGame
            {
                GroupId = selectedGroup.Id,
                Title = Title.Trim(),
                MinPlayers = minPlayersValue,
                MaxPlayers = maxPlayersValue,
                DurationMinutes = durationValue,
                GameGenre = string.IsNullOrWhiteSpace(GameGenre) ? null : GameGenre.Trim(),
                OwnerPlayerId = string.IsNullOrWhiteSpace(currentPlayerId) ? null : currentPlayerId
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
