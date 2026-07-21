using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

public partial class GameLibraryViewModel : ObservableObject
{
    private readonly BoardGameRepository _boardGameRepository;
    private readonly GroupOverviewRepository _groupOverviewRepository;
    private readonly CurrentPlayerService _currentPlayerService;

    public ObservableCollection<BoardGame> Games { get; } = new();

    public ObservableCollection<GamingGroupListItem> Groups { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    private GamingGroupListItem? selectedGroup;

    public GamingGroupListItem? SelectedGroup
    {
        get => selectedGroup;
        set
        {
            if (SetProperty(ref selectedGroup, value) && value is not null)
            {
                _ = LoadGamesCommand.ExecuteAsync(null);
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(errorMessage);

    public GameLibraryViewModel(
        BoardGameRepository boardGameRepository,
        GroupOverviewRepository groupOverviewRepository,
        CurrentPlayerService currentPlayerService)
    {
        _boardGameRepository = boardGameRepository;
        _groupOverviewRepository = groupOverviewRepository;
        _currentPlayerService = currentPlayerService;
    }

    [RelayCommand]
    public async Task LoadGamesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            errorMessage = null;

            if (Groups.Count == 0)
            {
                await LoadGroupsAsync();
            }

            Games.Clear();

            if (SelectedGroup is null)
            {
                errorMessage = "Keine aktive Gruppe gefunden. Bitte lege zuerst eine Gruppe an oder tritt einer Gruppe bei.";
                return;
            }

            var games = await _boardGameRepository.GetByGroupAsync(SelectedGroup.Id);

            foreach (var game in games)
            {
                Games.Add(game);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Die Spiele konnten nicht geladen werden. {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Die Spiele konnten nicht geladen werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        if (SelectedGroup is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Keine Gruppe ausgewählt",
                "Bitte wähle zuerst eine Gruppe aus.",
                "OK");

            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(AddGameView)}?groupId={Uri.EscapeDataString(SelectedGroup.Id)}");
    }

    [RelayCommand]
    private async Task DeleteGameAsync(BoardGame? game)
    {
        if (game is null)
            return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Spiel löschen",
            $"Möchtest du \"{game.Title}\" wirklich löschen?",
            "Löschen",
            "Abbrechen");

        if (!confirm)
            return;

        try
        {
            errorMessage = null;

            await _boardGameRepository.SoftDeleteAsync(game);

            Games.Remove(game);
        }
        catch (Exception ex)
        {
            errorMessage = $"Das Spiel konnte nicht gelöscht werden. {ex.Message}";

            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Das Spiel konnte nicht gelöscht werden.\n{ex.Message}",
                "OK");
        }
    }

    private async Task LoadGroupsAsync()
    {
        Groups.Clear();

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
        {
            errorMessage = "Es ist kein aktiver Spieler geladen.";
            return;
        }

        var groups = await _groupOverviewRepository.GetGroupsByPlayerIdAsync(currentPlayerId);

        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        SelectedGroup ??= Groups.FirstOrDefault();
    }

}
