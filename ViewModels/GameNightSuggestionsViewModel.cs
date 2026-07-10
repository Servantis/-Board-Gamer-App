using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

public partial class GameNightSuggestionsViewModel : ObservableObject
{
    private readonly IGameSuggestionRepository _suggestionRepository;
    private readonly BoardGameRepository _boardGameRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private GameNight? gameNight;

    public ObservableCollection<GameSuggestionListItem> Suggestions { get; } = new();

    public string EventInfoText
    {
        get
        {
            if (GameNight is null)
            {
                return "Kein Termin ausgewählt";
            }

            var parts = new[]
            {
            GameNight.GroupName,
            GameNight.LocationName,
            GameNight.HostName
        }
            .Where(part => !string.IsNullOrWhiteSpace(part));

            return string.Join(" · ", parts);
        }
    }

    public GameNightSuggestionsViewModel(
        IGameSuggestionRepository suggestionRepository,
        BoardGameRepository boardGameRepository,
        CurrentPlayerService currentPlayerService)
    {
        _suggestionRepository = suggestionRepository;
        _boardGameRepository = boardGameRepository;
        _currentPlayerService = currentPlayerService;
    }

    partial void OnGameNightChanged(GameNight? value)
    {
        OnPropertyChanged(nameof(EventInfoText));
    }

    public async Task InitializeAsync()
    {
        await LoadSuggestionsAsync();
    }

    [RelayCommand]
    private async Task LoadSuggestionsAsync()
    {
        if (GameNight is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentPlayerService.PlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Kein Spieler",
                "Es ist aktuell kein Spieler angemeldet.",
                "OK");

            return;
        }

        try
        {
            IsBusy = true;

            var suggestions = await _suggestionRepository.GetSuggestionsForGameNightAsync(
                GameNight.Id,
                _currentPlayerService.PlayerId);

            Suggestions.Clear();

            foreach (var suggestion in suggestions)
            {
                Suggestions.Add(suggestion);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleVoteAsync(GameSuggestionListItem? suggestion)
    {
        if (suggestion is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentPlayerService.PlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Kein Spieler",
                "Es ist aktuell kein Spieler angemeldet.",
                "OK");

            return;
        }

        await _suggestionRepository.ToggleVoteAsync(
            suggestion.SuggestionId,
            _currentPlayerService.PlayerId);

        await LoadSuggestionsAsync();
    }

    [RelayCommand]
    private async Task AddSuggestionAsync()
    {
        if (GameNight is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentPlayerService.PlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Kein Spieler",
                "Es ist aktuell kein Spieler angemeldet.",
                "OK");

            return;
        }

        var games = await _boardGameRepository.GetByGroupAsync(GameNight.GroupId);

        var availableGames = games
            .OrderBy(g => g.Title)
            .ToList();

        if (availableGames.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Keine Spiele",
                "In dieser Gruppe sind noch keine Spiele vorhanden.",
                "OK");

            return;
        }

        var titles = availableGames
            .Select(g => g.Title)
            .ToArray();

        var selectedTitle = await Shell.Current.DisplayActionSheetAsync(
            "Spiel vorschlagen",
            "Abbrechen",
            null,
            titles);

        if (string.IsNullOrWhiteSpace(selectedTitle) || selectedTitle == "Abbrechen")
        {
            return;
        }

        var selectedGame = availableGames.FirstOrDefault(g => g.Title == selectedTitle);

        if (selectedGame is null)
        {
            return;
        }

        var comment = await Shell.Current.DisplayPromptAsync(
            "Kommentar",
            "Optionaler Kommentar zum Spielvorschlag:",
            "Speichern",
            "Überspringen",
            "Kommentar");

        await _suggestionRepository.AddSuggestionAsync(
            GameNight.Id,
            selectedGame.Id,
            _currentPlayerService.PlayerId,
            comment);

        await LoadSuggestionsAsync();
    }
}