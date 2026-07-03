namespace BoardGamerApp.Views;

using CommunityToolkit.Maui.Views;
using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

/// <summary>
/// Code-Behind zur Hauptseite der Terminverwaltung (EventPage.xaml).
/// Diese Klasse enthält bewusst wenig Logik - die steckt im <see cref="EventViewModel"/>.
/// Hier passiert nur "UI-Klebstoff": Popup öffnen, Navigation auslösen, Daten beim
/// Erscheinen der Seite nachladen.
/// </summary>
public partial class EventPage : ContentPage
{
    // Das ViewModel wird der Page per Dependency Injection übergeben (siehe Konstruktor
    // und die Registrierung "builder.Services.AddTransient&lt;EventViewModel&gt;()" in
    // MauiProgram.cs). "BindingContext = ViewModel" verbindet dann die XAML-Bindings
    // ({Binding UpcomingGameNights} usw.) mit genau diesem ViewModel-Objekt.
    public EventViewModel ViewModel { get; set; }

    public EventPage(EventViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = ViewModel;
    }

    /// <summary>
    /// Wird von .NET MAUI automatisch aufgerufen, JEDES Mal wenn die Seite sichtbar wird
    /// (also auch beim Zurücknavigieren von einer anderen Seite). Deshalb ist das der
    /// richtige Ort, um die aktuellen Daten aus der Datenbank zu laden - so sind die
    /// Termine immer up-to-date, auch wenn sich zwischenzeitlich etwas geändert hat.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Erst die eigentlichen Termine laden, ...
        await ViewModel.LoadGameNightsAsync();

        // ... dann die Auswahllisten (Orte/Spiele/Spieler) für das "Neuer Termin"-Popup.
        // Das passiert schon hier und nicht erst beim Öffnen des Popups selbst, damit
        // die Picker sofort befüllt sind, sobald der Nutzer auf "+" tippt.
        await ViewModel.LoadReferenceDataAsync();
    }

    // Öffnet das Popup zum Erstellen eines neuen GameNight-Termins
    private void OnNewEventClicked(object sender, EventArgs e)
    {
        this.ShowPopup(new NewEventPopup(ViewModel));
    }

    // Navigation zur Seite mit vergangenen Events. Die komplette Liste der Termine
    // wird als Navigations-Parameter mitgegeben (siehe [QueryProperty] in
    // PreviousEventsPage.xaml.cs), dort wird dann nur noch nach "in der Vergangenheit"
    // gefiltert.
    private async void OnPreviousEventsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PreviousEventsPage),
            new Dictionary<string, object>
            {
                { "GameNights", ViewModel.GameNights }
            });
    }

    private async void OnGamesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GamesPage));
    }
}
