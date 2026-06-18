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
    private readonly DatabaseService _databaseService;

    public ObservableCollection<BoardGame> Games { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public GameLibraryViewModel(
        BoardGameRepository boardGameRepository,
        DatabaseService databaseService)
    {
        _boardGameRepository = boardGameRepository;
        _databaseService = databaseService;
    }

    [RelayCommand]
    public async Task LoadGamesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            Games.Clear();

            var games = await _boardGameRepository.GetAllAsync();

            foreach (var game in games)
            {
                Games.Add(game);
            }
        }
        catch (Exception ex)
        {
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
        await Shell.Current.GoToAsync(nameof(AddGameView));
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
            await _boardGameRepository.SoftDeleteAsync(game);

            Games.Remove(game);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Das Spiel konnte nicht gelöscht werden.\n{ex.Message}",
                "OK");
        }
    }

    private async Task<GamingGroup?> GetDefaultGroupAsync()
    {
        var groups = await _databaseService.GetNotDeletedAsync<GamingGroup>();
        return groups.FirstOrDefault();
    }
}