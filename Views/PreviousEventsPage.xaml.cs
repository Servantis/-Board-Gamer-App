namespace BoardGamerApp.Views;

using System.Collections.ObjectModel;
using BoardGamerApp.ViewModels;
using BoardGamerApp.Models;

/// <summary>
/// Zeigt die vergangenen Termine an. Diese Seite bekommt (anders als EventPage) KEIN
/// ViewModel per Dependency Injection, sondern die Liste aller Termine als
/// Navigations-Parameter von EventPage.OnPreviousEventsClicked() übergeben:
///
///   await Shell.Current.GoToAsync(nameof(PreviousEventsPage),
///       new Dictionary&lt;string, object&gt; { { "GameNights", ViewModel.GameNights } });
///
/// [QueryProperty(nameof(GameNights), "GameNights")] sagt .NET MAUI: "Sobald über
/// GoToAsync ein Parameter mit dem Schlüssel 'GameNights' ankommt, setze ihn in die
/// Property GameNights dieser Seite." Der zugehörige Property-Setter unten erstellt
/// daraus dann gleich das <see cref="PreviousEventsViewModel"/>.
/// </summary>
[QueryProperty(nameof(GameNights), "GameNights")]
public partial class PreviousEventsPage : ContentPage
{
    public ObservableCollection<GameNight> GameNights
    {
        set
        {
            // Aus der kompletten Terminliste baut das ViewModel selbst die gefilterte
            // Liste der vergangenen Termine (siehe PreviousEventsViewModel).
            BindingContext = new PreviousEventsViewModel(value);
        }
    }

    public PreviousEventsPage()
    {
        InitializeComponent();
    }

    private async void OnRatingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RatingPage));
    }
}
