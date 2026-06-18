using BoardGamerApp.Data;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BoardGamerApp.ViewModels;

public partial class GameLibraryViewModel : ObservableObject
{
    private readonly GameDatabase _database;
    private readonly IDialogService _dialogService;

    public ObservableCollection<BoardGame> Games { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public GameLibraryViewModel(GameDatabase database, IDialogService dialogService)
    {
        _database = database;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Games.Clear();

            List<BoardGame> gamesFromDatabase = await _database.GetGamesAsync();

            foreach (BoardGame game in gamesFromDatabase)
            {
                Games.Add(game);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Die Spiele konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddGameAsync()
    {
        await Shell.Current.GoToAsync(nameof(AddGamePage));
    }

    [RelayCommand]
    private async Task DeleteGameAsync(BoardGame ? game)
    {
        if (game is null || IsBusy)
            return;

        bool confirmed = await _dialogService.ConfirmAsync(
            "Spiel löschen",
            $"Möchtest du \"{game.Title}\" wirklich löschen?",
            "Löschen",
            "Abbrechen");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _database.DeleteGameAsync(game);

            Games.Remove(game);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Spiel konnte nicht gelöscht werden: {ex.Message}";

            await _dialogService.ShowAlertAsync(
                "Fehler",
                ErrorMessage,
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}