namespace BoardGamerApp.ViewModels;

using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using BoardGamerApp.Services.Interfaces;
using BoardGamerApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

/// <summary>
/// ViewModel für die Terminverwaltung (Views/EventPage.xaml, Views/NewEventPopup.xaml,
/// Views/MainPage.xaml).
///
/// Kurzer MVVM-Reminder:
/// - Die View (XAML) zeigt nur an und meldet Nutzer-Interaktionen (Klicks etc.).
/// - Das ViewModel hält den Zustand (welche Termine gibt es gerade?) und die Logik
///   (Termin speichern, löschen, ...). Es kennt die View NICHT.
/// - View und ViewModel sind über Data Binding verbunden: die XAML-Datei bindet z. B.
///   "{Binding UpcomingGameNights}" an eine CollectionView. Ändert sich die Collection hier,
///   aktualisiert sich die UI automatisch - ohne dass wir manuell irgendwas "neu zeichnen" müssen.
///
/// Diese Klasse erbt von <see cref="ObservableObject"/> (aus dem Community Toolkit MVVM-Paket).
/// Das gibt uns zwei nützliche Dinge:
/// 1. [ObservableProperty] auf einem privaten Feld erzeugt automatisch eine öffentliche
///    Property mit INotifyPropertyChanged-Benachrichtigung (siehe IsBusy unten).
/// 2. [RelayCommand] auf einer Methode erzeugt automatisch ein passendes ICommand
///    (z. B. wird aus "LoadGameNightsAsync()" die Property "LoadGameNightsCommand").
/// Das passiert alles per Source Generator im Hintergrund beim Kompilieren - deshalb
/// muss die Klasse "partial" sein.
/// </summary>
public partial class EventViewModel : ObservableObject
{
    // Diese Abhängigkeiten bekommt das ViewModel nicht selbst erzeugt, sondern
    // per Dependency Injection über den Konstruktor "hineingereicht" (siehe MauiProgram.cs,
    // wo EventViewModel registriert wird). Vorteil: Das ViewModel muss nicht wissen,
    // WIE die Datenbank-Verbindung aufgebaut wird, sondern nutzt einfach die fertigen Repositories.
    private readonly GameNightRepository _gameNightRepository;
    private readonly IGameSuggestionRepository _gameSuggestionRepository;
    private readonly BoardGameRepository _boardGameRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly DatabaseService _databaseService;
    private readonly CurrentPlayerService _currentPlayerService;
    private readonly IHostScheduleService _hostScheduleService;

    /// <summary>Alle geladenen Termine (vergangene und zukünftige), sortiert nach Datum.</summary>
    public ObservableCollection<GameNight> GameNights { get; } = new();

    /// <summary>Nur die Termine, die noch in der Zukunft liegen - wird auf EventPage angezeigt.</summary>
    public ObservableCollection<GameNight> UpcomingGameNights { get; } = new();

    // Datenquellen für die Auswahl-UI im "Neuer Termin"-Popup (kommen alle aus der DB,
    // damit dort keine inkonsistenten/freien Texte mehr eingegeben werden können).

    /// <summary>Gruppen, denen der aktuelle Spieler als aktives Mitglied angehört - zur Auswahl im "Neuer Termin"-Popup.</summary>
    public ObservableCollection<GamingGroup> Groups { get; } = new();

    /// <summary>Spiele der aktuell im Popup gewählten Gruppe, zur Auswahl im "Neuer Termin"-Popup.</summary>
    public ObservableCollection<BoardGame> Games { get; } = new();

    // [ObservableProperty] erzeugt daraus automatisch eine Property "IsBusy" (großgeschrieben)
    // inklusive Change-Notification. Wird benutzt, um doppeltes Laden zu verhindern und
    // könnte in der UI z. B. für einen Ladeindikator gebunden werden.
    [ObservableProperty]
    private bool isBusy;

    /// <summary>Alle Termine, deren Datum in der Vergangenheit liegt.</summary>
    public IEnumerable<GameNight> PastGameNights
        => GameNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now);

    /// <summary>
    /// Der chronologisch nächste anstehende Termin, an dem der aktuelle Spieler auch
    /// TATSÄCHLICH teilnimmt, oder null, falls keiner existiert - für die
    /// "Nächster Termin"-Vorschau-Karte auf der MainPage. Das ist entweder ein Termin,
    /// den der Spieler selbst hostet (er nimmt als Gastgeber automatisch teil), oder
    /// einer, dem er bereits zugesagt hat (siehe GameNight.MyAttendanceStatus). Termine
    /// mit einer noch offenen oder abgelehnten Antwort tauchen hier bewusst NICHT auf -
    /// dafür gibt es die separate "Noch offen"-Karte (siehe NextUnansweredGameNight).
    /// Abgesagte Termine (Status "cancelled", siehe GameNight.IsCancelled) werden
    /// ebenfalls übersprungen.
    /// </summary>
    public GameNight? NextUpcomingGameNight =>
        UpcomingGameNights
            .Where(n => !n.IsCancelled
                && (n.IsHostedByCurrentPlayer
                    || n.MyAttendanceStatus == BoardGamerConstants.AttendanceStatus.Accepted))
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

    /// <summary>
    /// True, wenn es einen anzeigbaren nächsten Termin gibt (für IsVisible-Bindings auf
    /// der MainPage) - bewusst über NextUpcomingGameNight statt über die reine Anzahl von
    /// UpcomingGameNights bestimmt, damit die Karte automatisch verschwindet, wenn alle
    /// künftigen Termine abgesagt sind.
    /// </summary>
    public bool HasUpcomingEvents => NextUpcomingGameNight is not null;

    /// <summary>
    /// Der chronologisch nächste anstehende Termin, zu dem der aktuelle Spieler ALS GAST
    /// (also explizit NICHT als Gastgeber) noch GAR NICHT geantwortet hat - für die zweite
    /// Vorschau-Karte auf der MainPage, die gezielt an eine noch offene Zusage/Absage
    /// erinnert. Der eigene Gastgeber ist hier bewusst DOPPELT ausgeschlossen
    /// (!IsHostedByCurrentPlayer zusätzlich zu CanRespondToAttendance, das dieselbe
    /// Bedingung eigentlich schon enthält) - so landet garantiert nie ein selbst gehosteter
    /// Termin auf dieser Karte, für den es ja ohnehin nichts zu entscheiden gibt.
    ///
    /// Ist NextUpcomingGameNight selbst bereits so ein Termin (dort steht ja ohnehin schon
    /// Zusagen/Absagen), wird er hier bewusst übersprungen, damit nicht zweimal dieselbe
    /// Karte erscheint - stattdessen zeigt diese Property dann den NÄCHSTEN Termin danach,
    /// der noch eine Antwort braucht.
    /// </summary>
    public GameNight? NextUnansweredGameNight =>
        UpcomingGameNights
            .Where(n => !n.IsHostedByCurrentPlayer
                && n.CanRespondToAttendance
                && string.IsNullOrWhiteSpace(n.MyAttendanceStatus))
            .Where(n => NextUpcomingGameNight is null || n.Id != NextUpcomingGameNight.Id)
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

    /// <summary>
    /// True, wenn es einen Termin gibt, auf den NextUnansweredGameNight zutrifft (für
    /// IsVisible-Bindings auf der MainPage).
    /// </summary>
    public bool HasUnansweredEvent => NextUnansweredGameNight is not null;

    /// <summary>Anzeigename des aktuell angemeldeten Spielers - für "Gastgeber: Du"-Anzeigen im Popup.</summary>
    public string? CurrentPlayerName => _currentPlayerService.PlayerName;

    /// <summary>
    /// Aktueller Filter für die Terminliste auf EventPage: "all" (alle künftigen Termine),
    /// BoardGamerConstants.GameNightStatus.Planned (nur noch geplante) oder .Cancelled
    /// (nur abgesagte - z. B. durch die automatische Mehrheits-Absage-Regel, siehe
    /// ApplyAttendanceInfoAsync). Wird über SetStatusFilterCommand gesetzt (siehe
    /// EventPage.xaml, Filter-Buttons). [NotifyPropertyChangedFor] sorgt dafür, dass sich
    /// FilteredUpcomingGameNights automatisch neu berechnet, sobald sich der Filter ändert.
    /// Default ist .Planned, damit beim Öffnen der EventPage direkt nur die noch
    /// geplanten (nicht die abgesagten) Termine zu sehen sind.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredUpcomingGameNights))]
    private string statusFilter = BoardGamerConstants.GameNightStatus.Planned;

    /// <summary>
    /// UpcomingGameNights, gefiltert nach StatusFilter. EventPage.xaml bindet die
    /// Terminliste an DIESE Property statt direkt an UpcomingGameNights, damit auch
    /// abgesagte künftige Termine je nach Filter sichtbar bleiben, statt komplett aus
    /// der Liste zu verschwinden.
    /// </summary>
    public IEnumerable<GameNight> FilteredUpcomingGameNights => StatusFilter switch
    {
        BoardGamerConstants.GameNightStatus.Planned =>
            UpcomingGameNights.Where(n => n.Status == BoardGamerConstants.GameNightStatus.Planned),
        BoardGamerConstants.GameNightStatus.Cancelled =>
            UpcomingGameNights.Where(n => n.Status == BoardGamerConstants.GameNightStatus.Cancelled),
        _ => UpcomingGameNights
    };

    public EventViewModel(
        GameNightRepository gameNightRepository,
        BoardGameRepository boardGameRepository,
        IGroupMemberRepository groupMemberRepository,
        IGameSuggestionRepository gameSuggestionRepository,
        DatabaseService databaseService,
        CurrentPlayerService currentPlayerService,
        IHostScheduleService hostScheduleService)
    {
        _gameNightRepository = gameNightRepository;
        _boardGameRepository = boardGameRepository;
        _groupMemberRepository = groupMemberRepository;
        _gameSuggestionRepository = gameSuggestionRepository;
        _databaseService = databaseService;
        _currentPlayerService = currentPlayerService;
        _hostScheduleService = hostScheduleService;
    }

    /// <summary>
    /// Lädt alle Termine aus den Gruppen, denen der aktuelle Spieler angehört, und baut
    /// zusätzlich die Anzeigenamen (LocationName/HostName/GameName) sowie die
    /// Zusagen/Absagen-Informationen (siehe <see cref="ApplyAttendanceInfoAsync"/>) zusammen.
    ///
    /// [RelayCommand] macht daraus automatisch eine Property "LoadGameNightsCommand",
    /// die man z. B. an einen Button binden könnte. Wir rufen die Methode hier aber auch
    /// direkt aus EventPage.OnAppearing() auf ("await ViewModel.LoadGameNightsAsync();") -
    /// beides ist möglich, [RelayCommand] ist nur ein zusätzliches Angebot für die UI.
    /// </summary>
    [RelayCommand]
    public async Task LoadGameNightsAsync()
    {
        // Verhindert, dass die Methode mehrfach gleichzeitig läuft (z. B. wenn der Nutzer
        // schnell mehrfach auf "Aktualisieren" tippen würde).
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var currentPlayerId = _currentPlayerService.PlayerId;

            // Nur Termine aus Gruppen laden, denen der aktuelle Spieler tatsächlich als
            // aktives Mitglied angehört - ist niemand angemeldet, bleibt die Liste leer.
            var myGroupIds = string.IsNullOrWhiteSpace(currentPlayerId)
                ? new HashSet<string>()
                : (await _groupMemberRepository.GetGroupsForPlayerAsync(currentPlayerId))
                    .Select(g => g.Id)
                    .ToHashSet();

            var nights = (await _gameNightRepository.GetAllAsync())
                .Where(n => myGroupIds.Contains(n.GroupId))
                .ToList();

            // Für die Anzeigenamen brauchen wir die "Nachschlage-Tabellen" komplett geladen:
            // gaming_groups/locations/players/games für Gruppen-/Ort-/Veranstalter-/Spiel-Titel
            // und game_suggestions, um herauszufinden, welches Spiel zu welchem Termin
            // vorgeschlagen wurde. attendance/group_members werden für die
            // Zusagen/Absagen-Auswertung gebraucht (siehe ApplyAttendanceInfoAsync).
            var groups = await _databaseService.GetNotDeletedAsync<GamingGroup>();
            var locations = await _databaseService.GetNotDeletedAsync<GameLocation>();
            var players = await _databaseService.GetNotDeletedAsync<Player>();
            var games = await _databaseService.GetNotDeletedAsync<BoardGame>();
            var suggestions = await _databaseService.GetNotDeletedAsync<GameSuggestion>();
            var votes = await _databaseService.GetNotDeletedAsync<GameVote>();
            var attendances = await _databaseService.GetNotDeletedAsync<Attendance>();
            var activeGroupMembers = (await _databaseService.GetNotDeletedAsync<GroupMember>())
                .Where(m => m.Status == BoardGamerConstants.GroupMemberStatus.Active)
                .ToList();

            // In ein Dictionary (Id -> Objekt) umwandeln, damit das Nachschlagen pro Termin
            // gleich schnell ist (O(1) statt jedes Mal die ganze Liste zu durchsuchen).
            var groupsById = groups.ToDictionary(g => g.Id);
            var locationsById = locations.ToDictionary(l => l.Id);
            var playersById = players.ToDictionary(p => p.Id);
            var gamesById = games.ToDictionary(g => g.Id);

            // Anzahl aktiver Mitglieder pro Gruppe (für den Zusagen-Prozentsatz zählt davon
            // der Gastgeber selbst nicht mit, siehe ApplyAttendanceInfoAsync).
            var activeMemberCountByGroup = activeGroupMembers
                .GroupBy(m => m.GroupId)
                .ToDictionary(g => g.Key, g => g.Count());

            var attendancesByNight = attendances
                .GroupBy(a => a.GameNightId)
                .ToDictionary(g => g.Key, g => g.ToList());

            GameNights.Clear();
            UpcomingGameNights.Clear();

            foreach (var night in nights.OrderBy(n => ParseDate(n.ScheduledAt)))
            {
                // Bevor der Termin angezeigt wird: prüfen, ob sein Kalendertag inzwischen
                // in der Vergangenheit liegt, und in diesem Fall den Status automatisch
                // (und dauerhaft in der DB!) auf "completed" setzen - siehe Methode weiter
                // unten für die genaue Regel.
                await ApplyCompletedStatusIfDueAsync(night);

                ApplyDisplayNames(night, groupsById, locationsById, playersById, gamesById, suggestions, votes);

                night.IsHostedByCurrentPlayer =
                    !string.IsNullOrWhiteSpace(currentPlayerId) && night.HostPlayerId == currentPlayerId;

                // Zusagen/Absagen auswerten und - falls mehr als die Hälfte der übrigen
                // Gruppenmitglieder abgesagt hat - den Termin automatisch canceln.
                var memberCount = activeMemberCountByGroup.GetValueOrDefault(night.GroupId, 0);
                var nightAttendances = attendancesByNight.GetValueOrDefault(night.Id, new List<Attendance>());
                await ApplyAttendanceInfoAsync(night, memberCount, nightAttendances, currentPlayerId);

                GameNights.Add(night);

                if (ParseDate(night.ScheduledAt) >= DateTime.Now)
                    UpcomingGameNights.Add(night);
            }

            // Bestimmt, welcher der geladenen Termine der chronologisch nächste ist, und
            // markiert genau diesen einen (IsNextUpcoming) - siehe RecomputeNextUpcoming().
            RecomputeNextUpcoming();

            // GameNights/UpcomingGameNights sind ObservableCollections und melden Änderungen
            // (Add/Remove/Clear) von selbst an die UI. Die Properties weiter oben
            // (PastGameNights, NextUpcomingGameNight, HasUpcomingEvents) sind aber nur
            // "berechnete" Properties (get-only, kein eigenes Feld) - für die muss man
            // die UI manuell benachrichtigen, siehe NotifyDerivedProperties().
            NotifyDerivedProperties();
        }
        catch (Exception ex)
        {
            // Statt die App abstürzen zu lassen, zeigen wir dem Nutzer eine verständliche
            // Fehlermeldung. Shell.Current.DisplayAlertAsync ist eine kleine Erweiterung
            // dieses Projekts rund um MAUI's DisplayAlert.
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Die Termine konnten nicht geladen werden.\n{ex.Message}",
                "OK");
        }
        finally
        {
            // "finally" stellt sicher, dass IsBusy IMMER wieder zurückgesetzt wird -
            // auch wenn oben eine Exception geflogen ist.
            IsBusy = false;
        }
    }

    /// <summary>
    /// Lädt die Gruppen, denen der aktuelle Spieler angehört, für den Gruppen-Picker im
    /// "Neuer Termin"-Popup. Wird von EventPage.OnAppearing() aufgerufen, BEVOR
    /// der Nutzer überhaupt auf "+" tippt - so ist die Liste schon bereit, sobald
    /// sich das Popup öffnet. Die Spiele-Auswahl wird NICHT hier geladen, sondern erst
    /// dynamisch pro gewählter Gruppe über <see cref="GetGamesForGroupAsync"/>, sobald
    /// im Popup eine Gruppe ausgewählt wird.
    /// </summary>
    public async Task LoadReferenceDataAsync()
    {
        Groups.Clear();
        Games.Clear();

        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return;

        var myGroups = await _groupMemberRepository.GetGroupsForPlayerAsync(currentPlayerId);

        foreach (var group in myGroups)
            Groups.Add(group);
    }

    /// <summary>
    /// Lädt die Spiele einer bestimmten Gruppe - wird vom "Neuer Termin"-Popup aufgerufen,
    /// sobald der Nutzer im Gruppen-Picker eine Gruppe auswählt, damit GamesCollectionView
    /// immer nur die Spiele DIESER Gruppe zur Auswahl anbietet.
    /// </summary>
    public async Task<List<BoardGame>> GetGamesForGroupAsync(string groupId)
    {
        return await _boardGameRepository.GetByGroupAsync(groupId);
    }

    /// <summary>
    /// Sucht den Ort, den der AKTUELLE Spieler in der angegebenen Gruppe als eigenen Ort
    /// hinterlegt hat (locations.owner_player_id). Der Gastgeber eines Termins ist immer
    /// der Ersteller - der Ort ergibt sich deshalb automatisch aus dessen eigenem Ort,
    /// statt frei ausgewählt zu werden. Liefert null, wenn der aktuelle Spieler in dieser
    /// Gruppe (noch) keinen eigenen Ort hat.
    /// </summary>
    public async Task<GameLocation?> GetOwnedLocationAsync(string groupId)
    {
        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId) || string.IsNullOrWhiteSpace(groupId))
            return null;

        var locations = await _databaseService.GetNotDeletedAsync<GameLocation>();

        return locations.FirstOrDefault(l => l.GroupId == groupId && l.OwnerPlayerId == currentPlayerId);
    }

    /// <summary>
    /// Speichert einen neuen Termin in der Datenbank. Gastgeber ist immer der aktuell
    /// angemeldete Spieler (siehe CurrentPlayerService) - eine freie Auswahl gibt es
    /// bewusst nicht mehr. Der Ort ergibt sich daraus automatisch (siehe
    /// <see cref="GetOwnedLocationAsync"/>): hat der aktuelle Spieler in der gewählten
    /// Gruppe keinen eigenen Ort hinterlegt, wird das Anlegen mit einer Fehlermeldung
    /// abgebrochen, statt den Termin ohne Ort zu speichern.
    ///
    /// Da die Tabelle game_nights selbst keine game_id-Spalte besitzt (ein Termin kann
    /// laut Datenmodell mehrere vorgeschlagene Spiele haben), werden die ausgewählten
    /// Spiele stattdessen über je einen eigenen Eintrag in der Tabelle game_suggestions
    /// verknüpft - "games" darf deshalb auch mehrere Einträge enthalten (Mehrfachauswahl
    /// im Popup, siehe NewEventPopup.xaml, GamesCollectionView).
    ///
    /// Gibt true zurück, wenn der Termin gespeichert wurde, und false, wenn das Anlegen
    /// abgebrochen wurde (nicht angemeldet, oder kein eigener Ort in der Gruppe) - der
    /// Aufrufer (NewEventPopup) nutzt das, um das Popup nur bei Erfolg zu schließen.
    /// </summary>
    public async Task<bool> AddGameNightAsync(
        GameNight night,
        GamingGroup group,
        IReadOnlyList<BoardGame> games)
    {
        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
        {
            await Shell.Current.DisplayAlertAsync(
                "Nicht angemeldet",
                "Es ist aktuell kein Spieler angemeldet. Der Termin kann nicht gespeichert werden.",
                "OK");

            return false;
        }

        var ownedLocation = await GetOwnedLocationAsync(group.Id);

        if (ownedLocation is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Kein eigener Ort hinterlegt",
                $"Du hast in der Gruppe \"{group.Name}\" noch keinen eigenen Ort hinterlegt. " +
                "Bitte lege zuerst einen Ort an, bevor du dort einen Termin erstellst.",
                "OK");

            return false;
        }

        try
        {
            night.GroupId = group.Id;
            night.LocationId = ownedLocation.Id;
            night.HostPlayerId = currentPlayerId;

            await _gameNightRepository.AddAsync(night);

            // Spielvorschläge werden zentral über das GameSuggestionRepository synchronisiert
            // (statt sie hart zu löschen und neu anzulegen) - dadurch bleiben spätere Votes
            // auf ein Spiel erhalten, auch wenn beim Bearbeiten mal kurz etwas anderes
            // ausgewählt war.
            await SyncGameSuggestionsForNightAsync(night, games, currentPlayerId);

            // Die Anzeigenamen setzen wir direkt hier, statt die ganze Liste neu aus der
            // DB zu laden - das spart einen kompletten Reload nur für einen neuen Termin.
            night.GroupName = group.Name;
            night.LocationName = ownedLocation.Name;
            night.HostName = _currentPlayerService.PlayerName;
            night.GameName = games.Count > 0 ? string.Join(", ", games.Select(g => g.Title)) : null;
            night.IsHostedByCurrentPlayer = true;

            // Neuer Termin hat direkt nach dem Erstellen noch keine Votes.
            // Deshalb gibt es hier noch keinen Favoriten.
            night.TopVotedGameName = null;
            night.TopVotedGameVoteCount = 0;

            GameNights.Add(night);

            if (ParseDate(night.ScheduledAt) >= DateTime.Now)
            {
                UpcomingGameNights.Add(night);
            }

            RecomputeNextUpcoming();
            NotifyDerivedProperties();

            return true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht gespeichert werden.\n{ex.Message}",
                "OK");

            return false;
        }
    }

    /// <summary>
    /// Aktualisiert einen bereits vorhandenen Termin (z. B. nach dem Antippen eines
    /// Termins auf EventPage und dem Bearbeiten im NewEventPopup). Gruppe, Gastgeber und
    /// Ort bleiben dabei unverändert - sie wurden beim Anlegen fest zugeordnet (siehe
    /// AddGameNightAsync) und lassen sich nachträglich nicht mehr ändern. Bearbeitbar
    /// sind nur Datum/Uhrzeit, Notizen und die vorgeschlagenen Spiele.
    ///
    /// Die bisherigen Spielvorschläge (game_suggestions) für diesen Termin werden
    /// komplett entfernt und - für jedes (wieder) ausgewählte Spiel - neu angelegt. Das
    /// ist einfacher, als bestehende Einträge einzeln "anzupassen", und stellt sicher,
    /// dass am Ende genau die Spiele als Vorschlag hinterlegt sind, die man beim
    /// Bearbeiten ausgewählt hat (auch bei Mehrfachauswahl im Popup).
    ///
    /// Am Ende wird bewusst die komplette Terminliste neu geladen (LoadGameNightsAsync()),
    /// statt nur die Anzeigenamen dieses einen Termins zu aktualisieren: GameNight
    /// implementiert kein INotifyPropertyChanged, d. h. Änderungen an einem Objekt, das
    /// schon in GameNights/UpcomingGameNights liegt, würden von der UI sonst nicht bemerkt.
    /// Außerdem kann sich durch eine Datumsänderung auch ändern, ob der Termin in Zukunft
    /// oder Vergangenheit gehört - ein kompletter Reload behandelt das automatisch mit.
    ///
    /// Gibt true zurück, wenn die Aktualisierung geklappt hat, und false bei einem Fehler -
    /// der Aufrufer (NewEventPopup) nutzt das, um das Popup nur bei Erfolg zu schließen.
    /// </summary>
    public async Task<bool> UpdateGameNightAsync(GameNight night, IReadOnlyList<BoardGame> games)
    {
        try
        {
            await _gameNightRepository.UpdateAsync(night);

            // Spielvorschläge auch hier über das GameSuggestionRepository synchronisieren
            // (siehe AddGameNightAsync) - SuggestedByPlayerId ist der (unveränderliche)
            // Gastgeber des Termins.
            await SyncGameSuggestionsForNightAsync(night, games, night.HostPlayerId ?? string.Empty);

            await LoadGameNightsAsync();

            return true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht aktualisiert werden.\n{ex.Message}",
                "OK");

            return false;
        }
    }

    /// <summary>
    /// Sucht heraus, welche Spiele aktuell für einen Termin vorgeschlagen sind (über die
    /// Tabelle game_suggestions), und lädt dafür die passenden BoardGame-Objekte direkt
    /// aus der Datenbank.
    /// </summary>
    public async Task<List<BoardGame>> GetSuggestedGamesAsync(GameNight night)
    {
        var suggestions = await _databaseService.GetNotDeletedAsync<GameSuggestion>();

        var suggestedGameIds = suggestions
            .Where(s => s.GameNightId == night.Id)
            .Select(s => s.GameId)
            .ToHashSet();

        if (suggestedGameIds.Count == 0)
            return new List<BoardGame>();

        var allGames = await _databaseService.GetNotDeletedAsync<BoardGame>();

        return allGames.Where(g => suggestedGameIds.Contains(g.Id)).ToList();
    }

    /// <summary>
    /// Speichert die Zusage/Absage des aktuell angemeldeten Spielers zu einem Termin
    /// (Tabelle attendance, UNIQUE-Constraint auf game_night_id+player_id - deshalb hier
    /// "insert oder update" statt immer neu einzufügen). Nach dem Speichern wird die
    /// komplette Terminliste neu geladen (siehe LoadGameNightsAsync), damit sowohl die
    /// eigene Anzeige (MyAttendanceStatus) als auch die Prozentanzeige und die
    /// automatische Absage-Regel sofort aktuell sind.
    /// </summary>
    public async Task RespondToAttendanceAsync(GameNight night, string status)
    {
        var currentPlayerId = _currentPlayerService.PlayerId;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            return;

        try
        {
            var existing = (await _databaseService.GetNotDeletedAsync<Attendance>())
                .FirstOrDefault(a => a.GameNightId == night.Id && a.PlayerId == currentPlayerId);

            if (existing is not null)
            {
                existing.Status = status;
                await _databaseService.UpdateAsync(existing);
            }
            else
            {
                var attendance = new Attendance
                {
                    GameNightId = night.Id,
                    PlayerId = currentPlayerId,
                    Status = status
                };

                await _databaseService.InsertAsync(attendance);
            }

            await LoadGameNightsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Deine Antwort konnte nicht gespeichert werden.\n{ex.Message}",
                "OK");
        }
    }

    /// <summary>
    /// Löscht (soft-delete) einen Termin. [RelayCommand] macht daraus die Property
    /// "DeleteEventCommand" - genau der Name, an den in EventPage.xaml das
    /// "Löschen"-SwipeItem gebunden ist (Command="{Binding ... DeleteEventCommand}").
    /// </summary>
    [RelayCommand]
    private async Task DeleteEventAsync(GameNight? night)
    {
        if (night is null)
            return;

        try
        {
            await _gameNightRepository.SoftDeleteAsync(night);

            GameNights.Remove(night);
            UpcomingGameNights.Remove(night);

            // War der gelöschte Termin der bisher hervorgehobene "nächste Termin", muss
            // jetzt ein anderer (oder gar keiner mehr) diese Markierung bekommen.
            RecomputeNextUpcoming();
            NotifyDerivedProperties();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht gelöscht werden.\n{ex.Message}",
                "OK");
        }
    }

    /// <summary>
    /// Wird aktuell nur zu Debug-Zwecken genutzt (Konsolenausgabe), wenn ein
    /// Termin in der Liste angetippt wird. Könnte später z. B. zu einer
    /// Detailansicht des Termins navigieren.
    /// </summary>
    [RelayCommand]
    private void EventClicked(GameNight? night)
    {
        if (night is null)
            return;

        Console.WriteLine(
            $"Event angeklickt: {night.GameName ?? night.Notes} bei {night.HostName} am {ParseDate(night.ScheduledAt)}"
        );
    }


    [RelayCommand]
    private async Task OpenSuggestionsAsync(GameNight? night)
    {
        if (night is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                "Es wurde kein Termin übergeben.",
                "OK");

            return;
        }

        await Shell.Current.GoToAsync(
            nameof(GameNightSuggestionsPage),
            new Dictionary<string, object>
            {
            { "GameNight", night }
            });
    }

    /// <summary>
    /// Wird über den "Termin absagen"-Button auf der Terminkarte ausgelöst (siehe
    /// EventPage.xaml, nur sichtbar für den Gastgeber, solange der Termin noch "planned"
    /// ist - siehe GameNight.CanCancelEventByHost). Fragt vorher per Sicherheitsabfrage
    /// nach, weil eine komplette Absage - anders als eine einzelne Zu-/Absage - alle
    /// Gruppenmitglieder betrifft und sich nicht rückgängig machen lässt.
    /// </summary>
    [RelayCommand]
    private async Task CancelEventAsync(GameNight? night)
    {
        if (night is null || !night.CanCancelEventByHost)
            return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Termin absagen",
            "Möchtest du diesen Termin wirklich ganz absagen? Alle Gruppenmitglieder sehen den Termin dann als abgesagt.",
            "Ja, absagen",
            "Abbrechen");

        if (!confirm)
            return;

        try
        {
            night.Status = BoardGamerConstants.GameNightStatus.Cancelled;
            await _gameNightRepository.UpdateAsync(night);

            // Kompletter Reload statt nur lokaler Anpassung, weil sich durch die Absage
            // auch NextUpcomingGameNight/FilteredUpcomingGameNights ändern können (siehe
            // GameNight.IsCancelled) - GameNight selbst hat kein INotifyPropertyChanged.
            await LoadGameNightsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht abgesagt werden.\n{ex.Message}",
                "OK");
        }
    }


    /// <summary>
    /// Prüft, ob ein Termin automatisch als "completed" (erledigt) markiert werden muss,
    /// und schreibt diesen Status-Wechsel bei Bedarf dauerhaft in die Datenbank.
    ///
    /// Regel: Sobald der KALENDERTAG des Termins (also ohne Uhrzeit - ein Termin um
    /// 20:00 Uhr gilt schon ab 00:00 Uhr des nächsten Tages als "vorbei") vor dem
    /// heutigen Tag liegt UND der Termin noch den Status "planned" hat, wird er auf
    /// "completed" gesetzt. Bereits stornierte Termine (Status "cancelled") werden
    /// dabei bewusst NICHT angefasst - eine Absage bleibt eine Absage, auch wenn das
    /// Datum inzwischen in der Vergangenheit liegt.
    ///
    /// ".Date" schneidet bei einem DateTime die Uhrzeit weg (z. B. wird aus
    /// "04.07.2026 20:00" einfach "04.07.2026 00:00") - genau das macht den Vergleich
    /// hier zu einem reinen Kalendertag-Vergleich statt einem exakten Zeitvergleich.
    /// </summary>
    private async Task ApplyCompletedStatusIfDueAsync(GameNight night)
    {
       // Debug.WriteLine("[EVENT] Automatischer Trigger");
        /*
        Debug.WriteLine(
      $"[EVENT] Prüfe GameNight " +
      $"{night.Id} | " +
      $"Date={night.ScheduledAt} | " +
      $"Status={night.Status}");
        */

        if (night.Status != BoardGamerConstants.GameNightStatus.Planned)
            return;

        if (ParseDate(night.ScheduledAt).Date >= DateTime.Now.Date)
        {
           // Debug.WriteLine( "[EVENT] Termin liegt noch in der Zukunft");

            return;
        }


        //Debug.WriteLine( $"[EVENT] Setze COMPLETED => {night.Id}");

        night.Status = BoardGamerConstants.GameNightStatus.Completed;

        await _gameNightRepository.UpdateAsync(night);
        // Debug.WriteLine( $"[EVENT] COMPLETED gespeichert => {night.Id}");

        // Debug.WriteLine( $"[EVENT] Starte Hostwechsel für Gruppe {night.GroupId}");

        await _hostScheduleService.EnsureNextHostExistsAsync(night.GroupId);

        await _hostScheduleService.ProcessHostChangeAsync(night.GroupId);

   

    }

    /// <summary>
    /// Wertet für einen Termin die Zusagen/Absagen (Tabelle attendance) aus und befüllt
    /// die Ignore-Properties MyAttendanceStatus/MyAttendanceStatusText/
    /// CanRespondToAttendance/AttendanceSummaryText (siehe GameNight.cs).
    ///
    /// Zwei unterschiedliche Zählweisen kommen hier bewusst zum Einsatz:
    /// - Für die ANZEIGE (AttendanceSummaryText) zählt der Gastgeber automatisch als
    ///   "zugesagt" mit (er nimmt ja ohnehin teil) - aus "2 von 2 befragten Mitgliedern
    ///   zugesagt" wird z. B. "3 von 3 Gruppenmitgliedern zugesagt", inklusive Gastgeber.
    /// - Für die AUTOMATISCHE ABSAGE-REGEL zählt weiterhin nur, wer tatsächlich befragt
    ///   wurde: "mehr als 50%" heißt hier - von den aktiven Gruppenmitgliedern OHNE den
    ///   Gastgeber (der wird ja nicht gefragt, ob der Termin bei ihm stattfindet) haben
    ///   mehr als die Hälfte abgesagt. Ist das der Fall und der Termin noch "planned",
    ///   wird er auf "cancelled" gesetzt und dauerhaft gespeichert - nach demselben
    ///   Prinzip wie ApplyCompletedStatusIfDueAsync, nur mit einer anderen Bedingung.
    /// </summary>
    private async Task ApplyAttendanceInfoAsync(
        GameNight night,
        int activeGroupMemberCount,
        List<Attendance> nightAttendances,
        string? currentPlayerId)
    {
        // Der Gastgeber zählt nicht als "zu befragendes" Mitglied - er nimmt ohnehin teil.
        var respondentCount = Math.Max(activeGroupMemberCount - 1, 0);

        var acceptedCount = nightAttendances.Count(a => a.Status == BoardGamerConstants.AttendanceStatus.Accepted);
        var declinedCount = nightAttendances.Count(a => a.Status == BoardGamerConstants.AttendanceStatus.Declined);

        night.MyAttendanceStatus = string.IsNullOrWhiteSpace(currentPlayerId)
            ? null
            : nightAttendances.FirstOrDefault(a => a.PlayerId == currentPlayerId)?.Status;

        night.MyAttendanceStatusText = night.MyAttendanceStatus switch
        {
            BoardGamerConstants.AttendanceStatus.Accepted => "Du hast zugesagt",
            BoardGamerConstants.AttendanceStatus.Declined => "Du hast abgesagt",
            _ => null
        };

        night.CanRespondToAttendance =
            !string.IsNullOrWhiteSpace(currentPlayerId)
            && night.Status == BoardGamerConstants.GameNightStatus.Planned
            && !night.IsHostedByCurrentPlayer;

        // Für die Anzeige zählt der Gastgeber mit dazu (er nimmt ja automatisch teil) -
        // aus "2/2 zugesagt" (nur die befragten Mitglieder) wird so z. B. "3/3 zugesagt"
        // (Gastgeber + alle befragten Mitglieder). activeGroupMemberCount enthält den
        // Gastgeber bereits als aktives Gruppenmitglied, deshalb reicht es, beim
        // Zähler (acceptedCount) den Gastgeber als "automatisch zugesagt" dazuzuzählen.
        // Für die automatische Absage-Regel unten bleibt es bewusst bei
        // respondentCount/declinedCount OHNE Gastgeber, weil dort nur die tatsächlich
        // befragten Mitglieder zählen sollen.
        var acceptedCountWithHost = acceptedCount + (activeGroupMemberCount > 0 ? 1 : 0);

        night.AttendanceSummaryText = activeGroupMemberCount > 0
            ? $"{Math.Round(100.0 * acceptedCountWithHost / activeGroupMemberCount)}% zugesagt ({acceptedCountWithHost}/{activeGroupMemberCount})"
            : null;

        // "Mehr als 50% abgesagt" ohne Fließkommazahlen geprüft: declinedCount/respondentCount > 0.5
        // ist gleichbedeutend mit declinedCount * 2 > respondentCount.
        if (night.Status == BoardGamerConstants.GameNightStatus.Planned
            && respondentCount > 0
            && declinedCount * 2 > respondentCount)
        {
            night.Status = BoardGamerConstants.GameNightStatus.Cancelled;
            await _gameNightRepository.UpdateAsync(night);
        }
    }

    /// <summary>
    /// Löst für einen einzelnen Termin die Foreign-Key-Ids (LocationId, HostPlayerId)
    /// sowie die zugehörigen game_suggestions in lesbare Anzeigenamen auf und schreibt
    /// sie in die [Ignore]-Properties LocationName/HostName/GameName des Termins
    /// (siehe GameNight.cs) - diese werden NICHT in der Datenbank gespeichert, sondern
    /// nur für die Anzeige in der UI gebraucht.
    /// </summary>
    private static void ApplyDisplayNames(
    GameNight night,
    Dictionary<string, GamingGroup> groupsById,
    Dictionary<string, GameLocation> locationsById,
    Dictionary<string, Player> playersById,
    Dictionary<string, BoardGame> gamesById,
    List<GameSuggestion> suggestions,
    List<GameVote> votes)
    {
        night.GroupName = groupsById.TryGetValue(night.GroupId, out var group)
            ? group.Name
            : null;

        night.LocationName = night.LocationId is not null && locationsById.TryGetValue(night.LocationId, out var location)
            ? location.Name
            : null;

        night.HostName = night.HostPlayerId is not null && playersById.TryGetValue(night.HostPlayerId, out var player)
            ? player.Name
            : null;

        var suggestionsForNight = suggestions
            .Where(s => s.GameNightId == night.Id)
            .ToList();

        var gameTitles = suggestionsForNight
            .Select(s => gamesById.TryGetValue(s.GameId, out var game) ? game.Title : null)
            .Where(title => title is not null)
            .ToList();

        night.GameName = gameTitles.Count > 0
            ? string.Join(", ", gameTitles)
            : null;

        ApplyTopVotedGame(night, suggestionsForNight, gamesById, votes);
    }


    private static void ApplyTopVotedGame(
    GameNight night,
    List<GameSuggestion> suggestionsForNight,
    Dictionary<string, BoardGame> gamesById,
    List<GameVote> votes)
    {
        night.TopVotedGameName = null;
        night.TopVotedGameVoteCount = 0;

        if (suggestionsForNight.Count == 0)
        {
            return;
        }

        var voteCounts = suggestionsForNight
            .Select(suggestion => new
            {
                Suggestion = suggestion,
                VoteCount = votes.Count(vote => vote.SuggestionId == suggestion.Id)
            })
            .ToList();

        var maxVotes = voteCounts.Max(x => x.VoteCount);

        if (maxVotes <= 0)
        {
            return;
        }

        var topGameTitles = voteCounts
            .Where(x => x.VoteCount == maxVotes)
            .Select(x => gamesById.TryGetValue(x.Suggestion.GameId, out var game) ? game.Title : null)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToList();

        if (topGameTitles.Count == 0)
        {
            return;
        }

        night.TopVotedGameName = string.Join(", ", topGameTitles);
        night.TopVotedGameVoteCount = maxVotes;
    }
    /// <summary>
    /// GameNights/UpcomingGameNights sind ObservableCollections und melden Änderungen
    /// (Add/Remove/Clear) automatisch an gebundene UI-Elemente. Die davon abgeleiteten,
    /// berechneten Properties (PastGameNights, NextUpcomingGameNight, HasUpcomingEvents)
    /// haben aber kein eigenes Backing-Feld und werden deshalb NICHT automatisch neu
    /// ausgewertet. OnPropertyChanged(nameof(...)) sagt der UI explizit: "diese Property
    /// hat sich (indirekt) geändert, bitte neu abfragen".
    /// </summary>
    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(PastGameNights));
        OnPropertyChanged(nameof(NextUpcomingGameNight));
        OnPropertyChanged(nameof(HasUpcomingEvents));
        OnPropertyChanged(nameof(NextUnansweredGameNight));
        OnPropertyChanged(nameof(HasUnansweredEvent));
        OnPropertyChanged(nameof(FilteredUpcomingGameNights));
    }

    /// <summary>
    /// Setzt den Filter für die Terminliste auf EventPage. [RelayCommand] macht daraus
    /// die Property "SetStatusFilterCommand", die die Filter-Buttons in EventPage.xaml
    /// mit ihrem jeweiligen CommandParameter (z. B. "all", "planned", "cancelled") aufrufen.
    /// </summary>
    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        StatusFilter = filter;
    }

    /// <summary>
    /// Ermittelt aus UpcomingGameNights den chronologisch frühesten NICHT abgesagten Termin
    /// (siehe GameNight.IsCancelled) und setzt bei diesem EINEN GameNight.IsNextUpcoming auf
    /// true, bei allen anderen auf false - abgesagte Termine kommen für diese Markierung
    /// nie in Frage, genau wie bei NextUpcomingGameNight.
    ///
    /// Wird nach jeder Änderung an UpcomingGameNights aufgerufen (Laden, Anlegen,
    /// Löschen), damit die Markierung immer den tatsächlich nächsten Termin trifft.
    /// </summary>
    private void RecomputeNextUpcoming()
    {
        var next = UpcomingGameNights
            .Where(n => !n.IsCancelled)
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

        foreach (var night in UpcomingGameNights)
            night.IsNextUpcoming = ReferenceEquals(night, next);
    }

    /// <summary>
    /// Wandelt den in der Datenbank gespeicherten ISO-8601-UTC-String (siehe
    /// GameNight.ScheduledAt) zurück in ein lokales DateTime, damit man z. B.
    /// "liegt der Termin in der Vergangenheit?" mit DateTime.Now vergleichen kann.
    /// </summary>
    private static DateTime ParseDate(string isoString)
    {
        return DateTime.Parse(isoString, null, System.Globalization.DateTimeStyles.RoundtripKind)
                       .ToLocalTime();
    }

    private async Task SyncGameSuggestionsForNightAsync(
    GameNight night,
    IReadOnlyList<BoardGame> selectedGames,
    string? suggestedByPlayerId)
    {
        var selectedGameIds = selectedGames
            .Select(game => game.Id)
            .ToHashSet();

        var existingSuggestions = (await _databaseService.GetNotDeletedAsync<GameSuggestion>())
            .Where(suggestion => suggestion.GameNightId == night.Id)
            .ToList();

        // Vorschläge, die nicht mehr ausgewählt sind, nur soft-deleten.
        // Dadurch bleiben vorhandene Votes in game_votes erhalten.
        foreach (var existingSuggestion in existingSuggestions)
        {
            if (!selectedGameIds.Contains(existingSuggestion.GameId))
            {
                await _gameSuggestionRepository.SoftDeleteSuggestionAsync(existingSuggestion.Id);
            }
        }

        if (selectedGames.Count == 0 || string.IsNullOrWhiteSpace(suggestedByPlayerId))
        {
            return;
        }

        var activeExistingGameIds = existingSuggestions
            .Where(suggestion => selectedGameIds.Contains(suggestion.GameId))
            .Select(suggestion => suggestion.GameId)
            .ToHashSet();

        foreach (var selectedGame in selectedGames)
        {
            if (activeExistingGameIds.Contains(selectedGame.Id))
            {
                continue;
            }

            await _gameSuggestionRepository.AddSuggestionAsync(
                night.Id,
                selectedGame.Id,
                suggestedByPlayerId,
                null);
        }
    }


}
