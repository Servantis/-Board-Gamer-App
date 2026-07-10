namespace BoardGamerApp.Views;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using BoardGamerApp.ViewModels;

/// <summary>
/// Zeigt die vergangenen Termine an - inklusive Bewertungs-Feature (siehe
/// PreviousEventsViewModel für die Details der drei möglichen Zustände pro Termin:
/// "bewerten", "bereits bewertet", "war selbst Gastgeber").
///
/// Diese Seite bekommt zwei Dinge:
/// 1. Per Dependency Injection (Konstruktor) den DatabaseService und den
///    CurrentPlayerService - beide werden für das Bewertungs-Feature gebraucht.
/// 2. Per Navigations-Parameter (siehe [QueryProperty]) die komplette Terminliste von
///    EventPage.OnPreviousEventsClicked().
///
/// Das Aufbauen des ViewModels ist asynchron (es lädt game_night_reviews aus der DB,
/// siehe PreviousEventsViewModel.InitializeAsync) - ein Property-Setter kann aber kein
/// "await" benutzen. Deshalb merkt der Setter von GameNights die Terminliste nur
/// zwischen, und das eigentliche Aufbauen des ViewModels passiert in OnAppearing() -
/// dem Seiten-Lebenszyklus-Ereignis, das MAUI jedes Mal aufruft, wenn diese Seite
/// sichtbar wird.
/// </summary>
[QueryProperty(nameof(GameNights), "GameNights")]
public partial class PreviousEventsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;

    // Die Terminliste kommt per Navigation an, BEVOR OnAppearing läuft - wir merken sie
    // uns hier zwischen, bis wir sie in OnAppearing tatsächlich verarbeiten können.
    private ObservableCollection<GameNight>? _pendingGameNights;

    public ObservableCollection<GameNight> GameNights
    {
        set => _pendingGameNights = value;
    }

    public PreviousEventsPage(
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;
    }

    /// <summary>
    /// Baut bei JEDEM Erscheinen der Seite ein frisches PreviousEventsViewModel auf, das
    /// die Bewertungs-Flags neu aus der Datenbank ermittelt. Das ist wichtig, damit z. B.
    /// direkt nach dem Zurückkommen von RatingPage (nachdem man gerade eine Bewertung
    /// abgegeben hat) das Badge korrekt "Bewertet" statt "Bewerten" anzeigt.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_pendingGameNights is null)
            return;

        var viewModel = new PreviousEventsViewModel(
            _pendingGameNights,
            _databaseService,
            _currentPlayerService);

        await viewModel.InitializeAsync();

        BindingContext = viewModel;
    }

    /// <summary>
    /// Wird beim Antippen eines vergangenen Termins ausgelöst. "sender" ist hier (wie
    /// schon bei EventPage.OnEditEventClicked) der TapGestureRecognizer selbst, dessen
    /// BindingContext automatisch der GameNight der angetippten Karte ist.
    ///
    /// Je nachdem, was PreviousEventsViewModel.InitializeAsync() für diesen Termin
    /// ermittelt hat (siehe GameNight.CanBeRatedByCurrentPlayer & Co.), passiert etwas
    /// anderes:
    /// - Darf bewertet werden -&gt; RatingPage öffnen (mit dem Termin als Parameter).
    /// - War der Nutzer selbst Gastgeber -&gt; kleine Zusammenfassung zeigen, wie er
    ///   bewertet wurde (Durchschnitt über alle Gastgeber-Bewertungen).
    /// - Schon bewertet -&gt; die eigene abgegebene Bewertung noch einmal anzeigen.
    /// - Alles andere (z. B. Termin technisch noch nicht "completed") -&gt; Hinweis.
    /// </summary>
    private async void OnRatingClicked(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not GameNight night)
            return;

        if (night.CanBeRatedByCurrentPlayer)
        {
            await Shell.Current.GoToAsync(nameof(RatingPage),
                new Dictionary<string, object>
                {
                    { "GameNight", night }
                });

            return;
        }

        if (night.IsHostedByCurrentPlayer)
        {
            await ShowHostSummaryAsync(night);
            return;
        }

        if (night.IsRatedByCurrentPlayer)
        {
            await ShowOwnReviewSummaryAsync(night);
            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Noch nicht möglich",
            "Dieser Termin kann aktuell nicht bewertet werden.",
            "OK");
    }

    /// <summary>
    /// Zeigt dem Gastgeber eines Termins eine kurze Zusammenfassung, wie er als
    /// Gastgeber bewertet wurde (Durchschnitt über alle abgegebenen Gastgeber-Sterne).
    /// Ein Gastgeber kann seinen eigenen Abend nicht selbst bewerten (siehe
    /// GameNight.CanBeRatedByCurrentPlayer), soll das Ergebnis aber sehen dürfen.
    /// </summary>
    private async Task ShowHostSummaryAsync(GameNight night)
    {
        var reviews = (await _databaseService.GetNotDeletedAsync<GameNightReview>())
            .Where(r => r.GameNightId == night.Id)
            .ToList();

        if (reviews.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Deine Bewertung",
                "Für diesen Abend liegen noch keine Bewertungen vor.",
                "OK");

            return;
        }

        // HostRating ist optional (int?) - es kann sein, dass jemand zwar Essen/Abend
        // bewertet, aber keine Gastgeber-Sterne vergeben hat. Deshalb hier nur die
        // Bewertungen zählen, bei denen HostRating tatsächlich gesetzt ist.
        var hostRatings = reviews
            .Where(r => r.HostRating.HasValue)
            .Select(r => r.HostRating!.Value)
            .ToList();

        var message = hostRatings.Count > 0
            ? $"Du wurdest von {hostRatings.Count} Person(en) als Gastgeber bewertet.\n" +
              $"Durchschnitt: {hostRatings.Average():0.0} von 5 Sternen"
            : $"{reviews.Count} Person(en) haben den Abend bewertet, " +
              "aber noch keine Gastgeber-Bewertung abgegeben.";

        await Shell.Current.DisplayAlertAsync("Deine Bewertung", message, "OK");
    }

    /// <summary>
    /// Zeigt dem AKTUELLEN Spieler seine eigene, schon abgegebene Bewertung für diesen
    /// Termin (Sterne für Gastgeber/Essen/Abend sowie den Kommentar) - dieselbe Idee wie
    /// <see cref="ShowHostSummaryAsync"/>, nur eben für die eigene statt für die
    /// erhaltenen Bewertungen.
    /// </summary>
    private async Task ShowOwnReviewSummaryAsync(GameNight night)
    {
        var currentPlayerId = _currentPlayerService.PlayerId;

        var review = (await _databaseService.GetNotDeletedAsync<GameNightReview>())
            .FirstOrDefault(r => r.GameNightId == night.Id && r.ReviewerPlayerId == currentPlayerId);

        if (review is null)
        {
            // Kann eigentlich nicht vorkommen, wenn IsRatedByCurrentPlayer true ist -
            // eine zusätzliche Absicherung schadet aber nicht.
            await Shell.Current.DisplayAlertAsync(
                "Deine Bewertung",
                "Zu dieser Bewertung konnten keine Details gefunden werden.",
                "OK");

            return;
        }

        var lines = new List<string> { $"Abend insgesamt: {FormatStars(review.OverallRating)}" };

        if (review.HostRating.HasValue)
            lines.Add($"Gastgeber: {FormatStars(review.HostRating.Value)}");

        if (review.FoodRating.HasValue)
            lines.Add($"Essen: {FormatStars(review.FoodRating.Value)}");

        if (!string.IsNullOrWhiteSpace(review.Comment))
            lines.Add($"Kommentar: \"{review.Comment}\"");

        await Shell.Current.DisplayAlertAsync(
            "Deine Bewertung",
            string.Join("\n", lines),
            "OK");
    }

    /// <summary>Wandelt eine 1-5-Sterne-Zahl in eine kleine Stern-Grafik aus Text um (z. B. "★★★★☆").</summary>
    private static string FormatStars(int rating)
    {
        return new string('★', rating) + new string('☆', 5 - rating);
    }
}
