namespace BoardGamerApp.ViewModels;

using System.Collections.ObjectModel;
using BoardGamerApp.Models;
using BoardGamerApp.Repositories;
using BoardGamerApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel für die Terminverwaltung (Views/EventPage.xaml, Views/NewEventPopup.xaml).
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
    // Diese vier Abhängigkeiten bekommt das ViewModel nicht selbst erzeugt, sondern
    // per Dependency Injection über den Konstruktor "hineingereicht" (siehe MauiProgram.cs,
    // wo EventViewModel registriert wird). Vorteil: Das ViewModel muss nicht wissen,
    // WIE die Datenbank-Verbindung aufgebaut wird, sondern nutzt einfach die fertigen Repositories.
    private readonly GameNightRepository _gameNightRepository;
    private readonly BoardGameRepository _boardGameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly DatabaseService _databaseService;

    /// <summary>Alle geladenen Termine (vergangene und zukünftige), sortiert nach Datum.</summary>
    public ObservableCollection<GameNight> GameNights { get; } = new();

    /// <summary>Nur die Termine, die noch in der Zukunft liegen - wird auf EventPage angezeigt.</summary>
    public ObservableCollection<GameNight> UpcomingGameNights { get; } = new();

    // Datenquellen für die Auswahl-UI im "Neuer Termin"-Popup (kommen alle aus der DB,
    // damit dort keine inkonsistenten/freien Texte mehr eingegeben werden können).
    // Die Picker in NewEventPopup.xaml binden ihre ItemsSource direkt an diese drei Listen.

    /// <summary>Orte der aktuellen Gruppe, zur Auswahl im "Neuer Termin"-Popup.</summary>
    public ObservableCollection<GameLocation> Locations { get; } = new();

    /// <summary>Spiele der aktuellen Gruppe, zur Auswahl im "Neuer Termin"-Popup.</summary>
    public ObservableCollection<BoardGame> Games { get; } = new();

    /// <summary>Aktive Spieler, zur Auswahl als Veranstalter im "Neuer Termin"-Popup.</summary>
    public ObservableCollection<Player> Players { get; } = new();

    // [ObservableProperty] erzeugt daraus automatisch eine Property "IsBusy" (großgeschrieben)
    // inklusive Change-Notification. Wird benutzt, um doppeltes Laden zu verhindern und
    // könnte in der UI z. B. für einen Ladeindikator gebunden werden.
    [ObservableProperty]
    private bool isBusy;

    /// <summary>Alle Termine, deren Datum in der Vergangenheit liegt.</summary>
    public IEnumerable<GameNight> PastGameNights
        => GameNights.Where(n => ParseDate(n.ScheduledAt) < DateTime.Now);

    /// <summary>Die nächsten drei anstehenden Termine - z. B. für eine kompakte Vorschau auf der MainPage.</summary>
    public IEnumerable<GameNight> Top3UpcomingGameNights =>
        UpcomingGameNights
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .Take(3);

    /// <summary>True, wenn mindestens ein zukünftiger Termin existiert (für IsVisible-Bindings).</summary>
    public bool HasUpcomingEvents => UpcomingGameNights.Count > 0;

    public EventViewModel(
        GameNightRepository gameNightRepository,
        BoardGameRepository boardGameRepository,
        IPlayerRepository playerRepository,
        DatabaseService databaseService)
    {
        _gameNightRepository = gameNightRepository;
        _boardGameRepository = boardGameRepository;
        _playerRepository = playerRepository;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Lädt alle Termine aus der Datenbank (über das Repository) und baut zusätzlich
    /// die Anzeigenamen (LocationName/HostName/GameName) zusammen - siehe
    /// <see cref="ApplyDisplayNames"/> für die Details dazu.
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

            var nights = await _gameNightRepository.GetAllAsync();

            // Für die Anzeigenamen brauchen wir die "Nachschlage-Tabellen" komplett geladen:
            // gaming_groups/locations/players/games für Gruppen-/Ort-/Veranstalter-/Spiel-Titel
            // und game_suggestions, um herauszufinden, welches Spiel zu welchem Termin
            // vorgeschlagen wurde.
            var groups = await _databaseService.GetNotDeletedAsync<GamingGroup>();
            var locations = await _databaseService.GetNotDeletedAsync<GameLocation>();
            var players = await _databaseService.GetNotDeletedAsync<Player>();
            var games = await _databaseService.GetNotDeletedAsync<BoardGame>();
            var suggestions = await _databaseService.GetNotDeletedAsync<GameSuggestion>();

            // In ein Dictionary (Id -> Objekt) umwandeln, damit das Nachschlagen pro Termin
            // gleich schnell ist (O(1) statt jedes Mal die ganze Liste zu durchsuchen).
            var groupsById = groups.ToDictionary(g => g.Id);
            var locationsById = locations.ToDictionary(l => l.Id);
            var playersById = players.ToDictionary(p => p.Id);
            var gamesById = games.ToDictionary(g => g.Id);

            GameNights.Clear();
            UpcomingGameNights.Clear();

            foreach (var night in nights.OrderBy(n => ParseDate(n.ScheduledAt)))
            {
                // Bevor der Termin angezeigt wird: prüfen, ob sein Kalendertag inzwischen
                // in der Vergangenheit liegt, und in diesem Fall den Status automatisch
                // (und dauerhaft in der DB!) auf "completed" setzen - siehe Methode weiter
                // unten für die genaue Regel.
                await ApplyCompletedStatusIfDueAsync(night);

                ApplyDisplayNames(night, groupsById, locationsById, playersById, gamesById, suggestions);

                GameNights.Add(night);

                if (ParseDate(night.ScheduledAt) >= DateTime.Now)
                    UpcomingGameNights.Add(night);
            }

            // Bestimmt, welcher der geladenen Termine der chronologisch nächste ist, und
            // markiert genau diesen einen (IsNextUpcoming) - siehe RecomputeNextUpcoming().
            RecomputeNextUpcoming();

            // GameNights/UpcomingGameNights sind ObservableCollections und melden Änderungen
            // (Add/Remove/Clear) von selbst an die UI. Die Properties weiter oben
            // (PastGameNights, Top3UpcomingGameNights, HasUpcomingEvents) sind aber nur
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
    /// Lädt die drei Auswahllisten (Orte/Spiele/Spieler), die das "Neuer Termin"-Popup
    /// für seine Picker braucht. Wird von EventPage.OnAppearing() aufgerufen, BEVOR
    /// der Nutzer überhaupt auf "+" tippt - so sind die Listen schon bereit, sobald
    /// sich das Popup öffnet.
    /// </summary>
    public async Task LoadReferenceDataAsync()
    {
        // "Gruppe" meint hier die Spielgruppe (gaming_groups) - aktuell nimmt die App
        // einfach die erste vorhandene Gruppe, weil es noch keine Gruppenauswahl/
        // Mitgliedschaftsprüfung gibt. Das wäre ein guter Punkt für eine spätere Erweiterung.
        var group = await GetDefaultGroupAsync();

        Locations.Clear();
        Games.Clear();
        Players.Clear();

        if (group is not null)
        {
            // Orte werden direkt über DatabaseService geladen und dann in-memory nach
            // GroupId gefiltert, weil es (noch) kein eigenes LocationRepository gibt.
            var locations = await _databaseService.GetNotDeletedAsync<GameLocation>();

            foreach (var location in locations
                .Where(l => l.GroupId == group.Id)
                .OrderBy(l => l.Name))
            {
                Locations.Add(location);
            }

            // Für Spiele gibt es bereits ein passendes Repository mit fertiger
            // Gruppen-Filterung - das nutzen wir hier direkt.
            var games = await _boardGameRepository.GetByGroupAsync(group.Id);

            foreach (var game in games)
                Games.Add(game);
        }

        // Spieler sind (anders als Orte/Spiele) nicht direkt an eine Gruppe gebunden,
        // deshalb reicht hier "alle aktiven Spieler" ohne Gruppenfilter.
        var activePlayers = await _playerRepository.GetActivePlayersAsync();

        foreach (var player in activePlayers)
            Players.Add(player);
    }

    /// <summary>
    /// Speichert einen neuen Termin in der Datenbank.
    ///
    /// Wichtig: location/game/host sind hier bereits existierende Objekte aus der
    /// Datenbank (ausgewählt über die Picker im Popup) - keine Freitexte! Dadurch
    /// können LocationId/HostPlayerId (Foreign Keys auf locations/players) niemals
    /// einen ungültigen Wert bekommen, der zu einem "FOREIGN KEY constraint failed"-
    /// Absturz führen würde.
    ///
    /// Da die Tabelle game_nights selbst keine game_id-Spalte besitzt (ein Termin kann
    /// laut Datenmodell mehrere vorgeschlagene Spiele haben), wird ein ausgewähltes Spiel
    /// stattdessen über einen zusätzlichen Eintrag in der Tabelle game_suggestions verknüpft.
    /// </summary>
    public async Task AddGameNightAsync(
        GameNight night,
        GameLocation? location,
        BoardGame? game,
        Player? host)
    {
        // Falls der Aufrufer (Popup) noch keine GroupId gesetzt hat, wird automatisch
        // die Standardgruppe verwendet. Gibt es gar keine Gruppe, brechen wir mit
        // einer verständlichen Meldung ab, statt einen DB-Fehler zu riskieren.
        GamingGroup? group;

        if (string.IsNullOrWhiteSpace(night.GroupId))
        {
            group = await GetDefaultGroupAsync();

            if (group is null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Keine Gruppe vorhanden",
                    "Es wurde keine Spielgruppe gefunden. Bitte lege zuerst eine Gruppe an.",
                    "OK");

                return;
            }

            night.GroupId = group.Id;
        }
        else
        {
            // War schon eine GroupId gesetzt, laden wir die Gruppe trotzdem einmal nach -
            // nur um gleich den Anzeigenamen (GroupName) für die UI zu haben.
            group = await _databaseService.GetByIdAsync<GamingGroup>(night.GroupId);
        }

        try
        {
            // location/host dürfen null sein (Ort/Veranstalter sind optional) -
            // der ?. -Operator sorgt dafür, dass wir dann einfach null zuweisen,
            // statt eine NullReferenceException zu riskieren.
            night.LocationId = location?.Id;
            night.HostPlayerId = host?.Id;

            await _gameNightRepository.AddAsync(night);

            // Wurde ein Spiel ausgewählt, legen wir zusätzlich einen game_suggestions-
            // Eintrag an. Diese Tabelle verlangt "wer hat das Spiel vorgeschlagen"
            // (SuggestedByPlayerId, NOT NULL) - wir nehmen dafür den Veranstalter,
            // und falls keiner gewählt wurde, ersatzweise den ersten verfügbaren Spieler.
            if (game is not null)
            {
                var suggestedByPlayerId = host?.Id ?? Players.FirstOrDefault()?.Id;

                if (!string.IsNullOrWhiteSpace(suggestedByPlayerId))
                {
                    var suggestion = new GameSuggestion
                    {
                        GameNightId = night.Id,
                        GameId = game.Id,
                        SuggestedByPlayerId = suggestedByPlayerId
                    };

                    await _databaseService.InsertAsync(suggestion);
                }
            }

            // Die Anzeigenamen setzen wir direkt hier, statt die ganze Liste neu aus der
            // DB zu laden - das spart einen kompletten Reload nur für einen neuen Termin.
            night.GroupName = group?.Name;
            night.LocationName = location?.Name;
            night.HostName = host?.Name;
            night.GameName = game?.Title;

            GameNights.Add(night);

            if (ParseDate(night.ScheduledAt) >= DateTime.Now)
                UpcomingGameNights.Add(night);

            RecomputeNextUpcoming();
            NotifyDerivedProperties();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht gespeichert werden.\n{ex.Message}",
                "OK");
        }
    }

    /// <summary>
    /// Aktualisiert einen bereits vorhandenen Termin (z. B. nach dem Antippen eines
    /// Termins auf EventPage und dem Bearbeiten im NewEventPopup). Im Unterschied zu
    /// <see cref="AddGameNightAsync"/> wird hier KEIN neuer Datensatz angelegt, sondern
    /// der übergebene "night" (gleiche Id!) in der Datenbank überschrieben.
    ///
    /// Der bisherige Spielvorschlag (game_suggestions) für diesen Termin wird komplett
    /// entfernt und - falls wieder ein Spiel gewählt wurde - neu angelegt. Das ist
    /// einfacher, als einen bestehenden Eintrag "anzupassen", und stellt sicher, dass
    /// pro Termin nie mehr als ein Vorschlag übrig bleibt.
    ///
    /// Am Ende wird bewusst die komplette Terminliste neu geladen (LoadGameNightsAsync()),
    /// statt nur die Anzeigenamen dieses einen Termins zu aktualisieren: GameNight
    /// implementiert kein INotifyPropertyChanged, d. h. Änderungen an einem Objekt, das
    /// schon in GameNights/UpcomingGameNights liegt, würden von der UI sonst nicht bemerkt.
    /// Außerdem kann sich durch eine Datumsänderung auch ändern, ob der Termin in Zukunft
    /// oder Vergangenheit gehört - ein kompletter Reload behandelt das automatisch mit.
    /// </summary>
    public async Task UpdateGameNightAsync(
        GameNight night,
        GameLocation? location,
        BoardGame? game,
        Player? host)
    {
        try
        {
            night.LocationId = location?.Id;
            night.HostPlayerId = host?.Id;

            await _gameNightRepository.UpdateAsync(night);

            // Alten Spielvorschlag/-vorschläge für diesen Termin entfernen ...
            var existingSuggestions = (await _databaseService.GetNotDeletedAsync<GameSuggestion>())
                .Where(s => s.GameNightId == night.Id)
                .ToList();

            foreach (var suggestion in existingSuggestions)
                await _databaseService.HardDeleteAsync(suggestion);

            // ... und, falls (wieder) ein Spiel ausgewählt ist, wie beim Anlegen einen
            // neuen Vorschlag anlegen (siehe AddGameNightAsync für dieselbe Logik).
            if (game is not null)
            {
                var suggestedByPlayerId = host?.Id ?? Players.FirstOrDefault()?.Id;

                if (!string.IsNullOrWhiteSpace(suggestedByPlayerId))
                {
                    var suggestion = new GameSuggestion
                    {
                        GameNightId = night.Id,
                        GameId = game.Id,
                        SuggestedByPlayerId = suggestedByPlayerId
                    };

                    await _databaseService.InsertAsync(suggestion);
                }
            }

            await LoadGameNightsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Fehler",
                $"Der Termin konnte nicht aktualisiert werden.\n{ex.Message}",
                "OK");
        }
    }

    /// <summary>
    /// Sucht heraus, welches Spiel aktuell für einen Termin vorgeschlagen ist (über die
    /// Tabelle game_suggestions) und gibt dafür direkt das passende Objekt aus der schon
    /// geladenen <see cref="Games"/>-Liste zurück (NICHT ein frisch aus der DB gelesenes,
    /// neues Objekt!). Das ist wichtig, damit NewEventPopup dieses Ergebnis 1:1 als
    /// GamePicker.SelectedItem verwenden kann: .NET MAUI erkennt eine Vorauswahl im
    /// Picker nur, wenn es sich um genau dasselbe Objekt (Referenz) handelt, das auch
    /// in der ItemsSource-Liste steckt.
    /// </summary>
    public async Task<BoardGame?> GetSuggestedGameAsync(GameNight night)
    {
        var suggestions = await _databaseService.GetNotDeletedAsync<GameSuggestion>();
        var suggestion = suggestions.FirstOrDefault(s => s.GameNightId == night.Id);

        if (suggestion is null)
            return null;

        return Games.FirstOrDefault(g => g.Id == suggestion.GameId);
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
        if (night.Status != BoardGamerConstants.GameNightStatus.Planned)
            return;

        if (ParseDate(night.ScheduledAt).Date >= DateTime.Now.Date)
            return;

        night.Status = BoardGamerConstants.GameNightStatus.Completed;

        await _gameNightRepository.UpdateAsync(night);
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
        List<GameSuggestion> suggestions)
    {
        // GroupId ist (anders als LocationId/HostPlayerId) NIE null - jeder Termin
        // gehört immer genau einer Gruppe (siehe GameNight.GroupId) - trotzdem hier
        // TryGetValue statt direktem Zugriff, für den (unwahrscheinlichen) Fall, dass
        // die Gruppe zwischenzeitlich gelöscht wurde.
        night.GroupName = groupsById.TryGetValue(night.GroupId, out var group)
            ? group.Name
            : null;

        // TryGetValue statt "locationsById[night.LocationId]", weil es durchaus sein kann,
        // dass LocationId null ist (kein Ort gewählt) oder theoretisch auf einen
        // inzwischen gelöschten Ort zeigt - in beiden Fällen wollen wir keinen Absturz,
        // sondern einfach "kein Name vorhanden" (null).
        night.LocationName = night.LocationId is not null && locationsById.TryGetValue(night.LocationId, out var location)
            ? location.Name
            : null;

        night.HostName = night.HostPlayerId is not null && playersById.TryGetValue(night.HostPlayerId, out var player)
            ? player.Name
            : null;

        // Ein Termin kann (laut Datenmodell) mehrere vorgeschlagene Spiele haben,
        // deshalb filtern wir hier alle passenden game_suggestions-Einträge heraus
        // und verbinden ihre Titel mit Komma - meistens wird das aber genau ein Titel sein.
        var gameTitles = suggestions
            .Where(s => s.GameNightId == night.Id)
            .Select(s => gamesById.TryGetValue(s.GameId, out var game) ? game.Title : null)
            .Where(title => title is not null);

        night.GameName = gameTitles.Any()
            ? string.Join(", ", gameTitles)
            : null;
    }

    /// <summary>
    /// GameNights/UpcomingGameNights sind ObservableCollections und melden Änderungen
    /// (Add/Remove/Clear) automatisch an gebundene UI-Elemente. Die davon abgeleiteten,
    /// berechneten Properties (PastGameNights, Top3UpcomingGameNights, HasUpcomingEvents)
    /// haben aber kein eigenes Backing-Feld und werden deshalb NICHT automatisch neu
    /// ausgewertet. OnPropertyChanged(nameof(...)) sagt der UI explizit: "diese Property
    /// hat sich (indirekt) geändert, bitte neu abfragen".
    /// </summary>
    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(PastGameNights));
        OnPropertyChanged(nameof(Top3UpcomingGameNights));
        OnPropertyChanged(nameof(HasUpcomingEvents));
    }

    /// <summary>
    /// Ermittelt aus UpcomingGameNights den chronologisch frühesten Termin und setzt bei
    /// diesem EINEN GameNight.IsNextUpcoming auf true, bei allen anderen auf false. Damit
    /// kann MainPage.xaml genau den nächsten anstehenden Termin optisch hervorheben
    /// (siehe dort: DataTrigger auf IsNextUpcoming).
    ///
    /// Wird nach jeder Änderung an UpcomingGameNights aufgerufen (Laden, Anlegen,
    /// Löschen), damit die Markierung immer den tatsächlich nächsten Termin trifft.
    /// </summary>
    private void RecomputeNextUpcoming()
    {
        var next = UpcomingGameNights
            .OrderBy(n => ParseDate(n.ScheduledAt))
            .FirstOrDefault();

        foreach (var night in UpcomingGameNights)
            night.IsNextUpcoming = ReferenceEquals(night, next);
    }

    /// <summary>
    /// Liefert die "Standardgruppe" der App. Aktuell gibt es noch keine echte
    /// Gruppenauswahl/-verwaltung für den eingeloggten Spieler, deshalb wird
    /// einfach die erste, nicht gelöschte Gruppe aus gaming_groups genommen.
    /// </summary>
    private async Task<GamingGroup?> GetDefaultGroupAsync()
    {
        var groups = await _databaseService.GetNotDeletedAsync<GamingGroup>();
        return groups.FirstOrDefault();
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
}
