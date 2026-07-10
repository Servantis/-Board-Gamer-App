namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// RatingStar (das kleine Hilfsmodell für einen einzelnen Stern) liegt im Projekt-Root-
// Namespace "BoardGamerApp" (siehe Models/RatingStar.cs) statt in "BoardGamerApp.Models" -
// deshalb hier ein expliziter using, statt uns auf automatisches Auflösen über
// übergeordnete Namespaces zu verlassen.
using BoardGamerApp;

/// <summary>
/// ViewModel für die Bewertungsseite (Views/RatingPage.xaml). Ein Spieler bewertet hier
/// nach einem abgeschlossenen Termin drei Dinge mit 1-5 Sternen: Gastgeber (optional),
/// Essen (optional) und den Abend insgesamt (Pflicht) - dazu ein optionaler Kommentar.
/// Gespeichert wird das Ganze als ein Datensatz in der Tabelle game_night_reviews
/// (siehe Models/GameNightReview.cs).
///
/// Genau wie EventViewModel erbt diese Klasse von <see cref="ObservableObject"/>
/// (CommunityToolkit.Mvvm): [ObservableProperty] erzeugt automatisch die öffentlichen
/// Properties samt Change-Notification, [RelayCommand] erzeugt aus SubmitAsync()
/// automatisch die Property "SubmitCommand", die der Speichern-Button in der XAML bindet.
///
/// Diese Klasse wird (wie EventViewModel) per Dependency Injection erzeugt (siehe
/// MauiProgram.cs) und bekommt dabei DatabaseService und CurrentPlayerService injiziert -
/// dadurch kennt sie sowohl die Datenbank als auch den aktuell angemeldeten Spieler.
/// </summary>
public partial class RatingViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;

    // Der Termin, der gerade bewertet wird - kommt über InitializeAsync() von
    // RatingPage.xaml.cs (dorthin wiederum per Navigations-Parameter von
    // PreviousEventsPage, siehe [QueryProperty] in RatingPage.xaml.cs).
    private GameNight? _gameNight;

    public ObservableCollection<RatingStar> RatingGastgeberItems { get; }
    public ObservableCollection<RatingStar> RatingEssenItems { get; }
    public ObservableCollection<RatingStar> RatingAbendItems { get; }

    // [ObservableProperty] erzeugt hieraus automatisch eine Property "RatingGastgeber"
    // (großgeschrieben). "partial void OnRatingGastgeberChanged(...)" weiter unten wird vom
    // Source-Generator automatisch NACH jeder Änderung dieser Property aufgerufen, um die
    // passenden Stern-Bilder in RatingGastgeberItems zu aktualisieren.
    [ObservableProperty]
    private int ratingGastgeber;

    [ObservableProperty]
    private int ratingEssen;

    [ObservableProperty]
    private int ratingAbend;

    /// <summary>Freiwilliger Freitext-Kommentar zur Bewertung.</summary>
    [ObservableProperty]
    private string? comment;

    /// <summary>Kurzer Kontext-Hinweis oben auf der Seite, z. B. "12.07.2026 bei Anna".</summary>
    [ObservableProperty]
    private string headerText = string.Empty;

    // Verhindert doppeltes Speichern, falls jemand sehr schnell zweimal auf den
    // Speichern-Button tippt, während der erste Speichervorgang noch läuft.
    [ObservableProperty]
    private bool isBusy;

    public RatingViewModel(
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService)
    {
        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;

        RatingGastgeberItems = CreateRatingCollection(starValue => RatingGastgeber = starValue);
        RatingEssenItems = CreateRatingCollection(starValue => RatingEssen = starValue);
        RatingAbendItems = CreateRatingCollection(starValue => RatingAbend = starValue);
    }

    /// <summary>
    /// Wird von RatingPage.OnAppearing() aufgerufen, sobald der zu bewertende Termin
    /// (per Navigation) bekannt ist. Prüft der Reihe nach die drei Voraussetzungen aus
    /// der User Story - ist eine davon nicht erfüllt, wird eine verständliche Meldung
    /// gezeigt und automatisch eine Seite zurücknavigiert, statt das (dann unpassende)
    /// Bewertungsformular anzuzeigen:
    ///
    /// 1. Es muss überhaupt ein Spieler mit dieser App-Installation verknüpft sein.
    /// 2. Der Termin muss abgeschlossen sein (Status "completed") - vorher ergibt eine
    ///    Bewertung fachlich keinen Sinn.
    /// 3. Der aktuelle Spieler darf nicht selbst der Gastgeber dieses Termins sein.
    /// 4. Der aktuelle Spieler darf diesen Termin noch nicht bewertet haben (die
    ///    Datenbank erlaubt sowieso nur EINEN Bewertungs-Eintrag pro Termin+Spieler).
    ///
    /// Normalerweise sollten diese Fälle schon auf PreviousEventsPage abgefangen werden
    /// (dort wird ja auch entschieden, ob überhaupt zu RatingPage navigiert wird) - diese
    /// Prüfung hier ist bewusst eine zusätzliche Absicherung ("defense in depth"), falls
    /// RatingPage doch einmal auf einem anderen Weg geöffnet wird.
    /// </summary>
    public async Task InitializeAsync(GameNight night)
    {
        _gameNight = night;

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Kein Spieler ausgewählt",
                "Mit dieser App-Installation ist aktuell kein Spieler verknüpft.",
                "OK");

            await Shell.Current.GoToAsync("..");
            return;
        }

        if (night.Status != BoardGamerConstants.GameNightStatus.Completed)
        {
            await Shell.Current.DisplayAlertAsync(
                "Noch nicht bewertbar",
                "Dieser Termin ist noch nicht abgeschlossen und kann deshalb noch nicht bewertet werden.",
                "OK");

            await Shell.Current.GoToAsync("..");
            return;
        }

        if (night.HostPlayerId == currentPlayerId)
        {
            await Shell.Current.DisplayAlertAsync(
                "Nicht möglich",
                "Du warst der Gastgeber dieses Termins und kannst deinen eigenen Abend nicht bewerten.",
                "OK");

            await Shell.Current.GoToAsync("..");
            return;
        }

        var existingReviews = await _databaseService.GetNotDeletedAsync<GameNightReview>();

        var alreadyRated = existingReviews.Any(r =>
            r.GameNightId == night.Id && r.ReviewerPlayerId == currentPlayerId);

        if (alreadyRated)
        {
            await Shell.Current.DisplayAlertAsync(
                "Bereits bewertet",
                "Du hast diesen Abend schon bewertet - danke dir!",
                "OK");

            await Shell.Current.GoToAsync("..");
            return;
        }

        HeaderText = BuildHeaderText(night);
    }

    /// <summary>
    /// Speichert die Bewertung als neuen Eintrag in game_night_reviews. [RelayCommand]
    /// macht daraus die Property "SubmitCommand", an die der Speichern-Button in
    /// RatingPage.xaml gebunden ist ("Command="{Binding SubmitCommand}"").
    /// </summary>
    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_gameNight is null || IsBusy)
            return;

        // overall_rating ist in der Datenbank NOT NULL (siehe game_night_reviews-Tabelle) -
        // "Abend" muss also mindestens 1 Stern haben. Gastgeber/Essen bleiben optional:
        // 0 Sterne (= gar nicht angetippt) wird beim Speichern zu "null".
        if (RatingAbend == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Bitte bewerten",
                "Bitte gib mindestens eine Sterne-Bewertung für den Abend insgesamt ab.",
                "OK");

            return;
        }

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return; // Sollte durch InitializeAsync() eigentlich schon abgefangen sein.

        try
        {
            IsBusy = true;

            var review = new GameNightReview
            {
                GameNightId = _gameNight.Id,
                ReviewerPlayerId = currentPlayerId,
                ReviewedHostPlayerId = _gameNight.HostPlayerId,
                HostRating = RatingGastgeber == 0 ? null : RatingGastgeber,
                FoodRating = RatingEssen == 0 ? null : RatingEssen,
                OverallRating = RatingAbend,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment
            };

            await _databaseService.InsertAsync(review);

            await Shell.Current.DisplayAlertAsync(
                "Danke!",
                "Deine Bewertung wurde gespeichert.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Die Bewertung konnte nicht gespeichert werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ObservableCollection<RatingStar> CreateRatingCollection(Action<int> setRating)
    {
        var list = new ObservableCollection<RatingStar>();

        for (int i = 1; i <= 5; i++)
        {
            int starValue = i;

            list.Add(new RatingStar
            {
                Image = "star_empty.png",
                TapCommand = new Command(() => setRating(starValue))
            });
        }

        return list;
    }

    private void UpdateStars(ObservableCollection<RatingStar> items, int rating)
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Image = (i < rating)
                ? "star_filled.png"
                : "star_empty.png";
        }
    }

    // Diese drei "On<Property>Changed"-Methoden werden vom [ObservableProperty]-Source-
    // Generator automatisch erkannt und NACH jeder Änderung der jeweiligen Property
    // aufgerufen (Namenskonvention: OnRatingGastgeberChanged für die Property
    // "RatingGastgeber" usw.) - dadurch aktualisieren sich die Stern-Bilder automatisch,
    // sobald der Nutzer auf einen Stern tippt.
    partial void OnRatingGastgeberChanged(int value) => UpdateStars(RatingGastgeberItems, value);
    partial void OnRatingEssenChanged(int value) => UpdateStars(RatingEssenItems, value);
    partial void OnRatingAbendChanged(int value) => UpdateStars(RatingAbendItems, value);

    private static string BuildHeaderText(GameNight night)
    {
        var date = DateTime.Parse(
            night.ScheduledAt,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind
        ).ToLocalTime().ToString("dd.MM.yyyy");

        return night.HostName is not null
            ? $"{date} bei {night.HostName}"
            : date;
    }
}
