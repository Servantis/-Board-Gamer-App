using BoardGamerApp.Data;
using BoardGamerApp.Models;
using System.Collections.ObjectModel;

namespace BoardGamerApp.Views;

public partial class GameLibrary : ContentPage
{
    private readonly GameDatabase _database;

    public ObservableCollection<games> Games { get; } = new();

    public GameLibrary(GameDatabase database)
    {
        InitializeComponent();

        _database = database;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadGamesAsync();
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            Games.Clear();

            var gamesFromDatabase = await _database.GetGamesAsync();

            foreach (var game in gamesFromDatabase)
            {
                Games.Add(game);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Die Spiele konnten nicht geladen werden: {ex.Message}",
                "OK");
        }
    }
}