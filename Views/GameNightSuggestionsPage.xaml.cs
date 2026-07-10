using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GameNightSuggestionsPage : ContentPage, IQueryAttributable
{
    private readonly GameNightSuggestionsViewModel _viewModel;

    public GameNightSuggestionsPage(GameNightSuggestionsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("GameNight", out var value))
        {
            await DisplayAlertAsync(
                "Fehler",
                "Es wurde kein Termin an die Spielvorschläge-Seite übergeben.",
                "OK");

            return;
        }

        if (value is not GameNight gameNight)
        {
            await DisplayAlertAsync(
                "Fehler",
                "Der übergebene Navigationsparameter ist kein gültiger Termin.",
                "OK");

            return;
        }

        _viewModel.GameNight = gameNight;

        await _viewModel.InitializeAsync();
    }
}