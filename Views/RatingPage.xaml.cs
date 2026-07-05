namespace BoardGamerApp.Views;

using BoardGamerApp.Models;
using BoardGamerApp.ViewModels;

/// <summary>
/// Seite zum Bewerten eines abgeschlossenen Termins (Gastgeber/Essen/Abend + Kommentar).
/// Wird von PreviousEventsPage.OnRatingClicked() geöffnet und bekommt dabei den zu
/// bewertenden Termin als Navigations-Parameter mit ("GameNight", siehe [QueryProperty]
/// unten) - genau das gleiche Muster, das schon PreviousEventsPage selbst für ihre
/// Terminliste ("GameNights") benutzt.
///
/// Das ViewModel (RatingViewModel) bekommt diese Seite per Dependency Injection
/// (Konstruktor) - dafür müssen RatingViewModel UND RatingPage in MauiProgram.cs als
/// Transient registriert sein (siehe dort).
/// </summary>
[QueryProperty(nameof(GameNight), "GameNight")]
public partial class RatingPage : ContentPage
{
    public RatingViewModel ViewModel { get; }

    // Der Termin kommt per Navigation an, BEVOR OnAppearing läuft - wir merken ihn uns
    // hier zwischen (ein Property-Setter kann kein "await" benutzen, das eigentliche
    // Verarbeiten passiert deshalb erst in OnAppearing).
    private GameNight? _pendingGameNight;

    public GameNight GameNight
    {
        set => _pendingGameNight = value;
    }

    public RatingPage(RatingViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_pendingGameNight is not null)
        {
            // Prüft die Voraussetzungen (Termin abgeschlossen? nicht selbst Gastgeber?
            // noch nicht bewertet?) und bereitet das Formular vor - siehe
            // RatingViewModel.InitializeAsync für die Details.
            await ViewModel.InitializeAsync(_pendingGameNight);
        }
    }
}
