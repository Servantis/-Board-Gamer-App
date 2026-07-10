namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;
using BoardGamerApp.Services;

/// <summary>
/// ViewModel für PreviousEventsPage: bekommt im Konstruktor die komplette Terminliste
/// (kommt per Navigation von EventPage, siehe PreviousEventsPage.xaml.cs) und filtert
/// daraus nur die Termine heraus, deren Datum in der Vergangenheit liegt.
///
/// Zusätzlich zum reinen Filtern übernimmt <see cref="InitializeAsync"/> auch das
/// Bewertungs-Feature: für jeden vergangenen Termin wird bestimmt, ob der AKTUELLE Spieler
/// (siehe CurrentPlayerService) diesen Termin bewerten darf, ihn schon bewertet hat, oder
/// selbst der Gastgeber war (siehe GameNight.CanBeRatedByCurrentPlayer & Co.). Dafür lädt
/// diese Klasse zusätzlich zur übergebenen Terminliste auch selbst Daten aus der Datenbank
/// (game_night_reviews) - deshalb ist InitializeAsync() asynchron.
/// </summary>
public class PreviousEventsViewModel
{
    private readonly IEnumerable<GameNight> _allNights;
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;

    public ObservableCollection<GameNight> PreviousEvents { get; } = new();

    public PreviousEventsViewModel(
        IEnumerable<GameNight> allNights,
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService)
    {
        _allNights = allNights;
        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;
    }

    /// <summary>
    /// Baut die gefilterte Liste (PreviousEvents) auf und befüllt dabei pro Termin die
    /// vier Bewertungs-Flags auf GameNight (siehe GameNight.cs). Wird von
    /// PreviousEventsPage.OnAppearing() aufgerufen - jedes Mal, wenn die Seite erscheint,
    /// damit z. B. nach einer neu abgegebenen Bewertung das Badge sofort aktualisiert ist.
    /// </summary>
    public async Task InitializeAsync()
    {
        PreviousEvents.Clear();

        var currentPlayerId = _currentPlayerService.PlayerId;

        // Alle (nicht gelöschten) Bewertungen laden - wir brauchen daraus nur die
        // Ids der Termine, die der AKTUELLE Spieler bereits bewertet hat.
        var reviews = await _databaseService.GetNotDeletedAsync<GameNightReview>();

        var ratedNightIds = string.IsNullOrWhiteSpace(currentPlayerId)
            ? new HashSet<string>()
            : reviews
                .Where(r => r.ReviewerPlayerId == currentPlayerId)
                .Select(r => r.GameNightId)
                .ToHashSet();

        foreach (var night in _allNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now))
        {
            var isLoggedIn = !string.IsNullOrWhiteSpace(currentPlayerId);
            var isHost = isLoggedIn && night.HostPlayerId == currentPlayerId;
            var isRated = ratedNightIds.Contains(night.Id);
            var isCompleted = night.Status == BoardGamerConstants.GameNightStatus.Completed;

            night.IsHostedByCurrentPlayer = isHost;
            night.IsRatedByCurrentPlayer = isRated;
            night.CanBeRatedByCurrentPlayer = isLoggedIn && isCompleted && !isHost && !isRated;

            // Welcher Badge-Text angezeigt wird, richtet sich nach genau einem der drei
            // Zustände - "isHost" wird zuerst geprüft, weil ein Gastgeber (auch falls er
            // aus irgendeinem Grund schon einen Review-Eintrag hätte) IMMER "Deine
            // Bewertung" sehen soll, nie ein normales "Bewerten"/"Bewertet".
            night.RatingBadgeText = night.CanBeRatedByCurrentPlayer
                ? "Bewerten"
                : isHost
                    ? "Deine Bewertung"
                    : isRated
                        ? "Bewertet"
                        : null;

            PreviousEvents.Add(night);
        }
    }

    // Gleiche Hilfsmethode wie in EventViewModel: wandelt den gespeicherten
    // ISO-8601-UTC-String zurück in ein lokales DateTime zum Vergleichen.
    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(
            isoString,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind
        ).ToLocalTime();
    }
}
