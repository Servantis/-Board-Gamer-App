using SQLite;

namespace BoardGamerApp.Models;

/// <summary>
/// Repräsentiert einen Termin ("Spieleabend") in der Terminverwaltung.
/// Diese Klasse wird von SQLite-net 1:1 auf die Tabelle <c>game_nights</c> abgebildet:
/// jede Property mit [Column("...")] entspricht einer Spalte in der SQLite-Datenbank.
///
/// Die Basisklasse <see cref="BaseSyncEntity"/> liefert bereits die Standard-Felder,
/// die jede Tabelle im Sync-Konzept dieser App braucht: Id, CreatedAt, UpdatedAt,
/// DeletedAt (Soft-Delete statt echtem Löschen) und Version.
/// </summary>
[Table("game_nights")]
public class GameNight : BaseSyncEntity
{
    /// <summary>
    /// Foreign Key auf die Gruppe (gaming_groups), zu der dieser Termin gehört.
    /// Ein Termin gehört immer genau einer Spielgruppe.
    /// </summary>
    [Indexed]
    [NotNull]
    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Datum + Uhrzeit des Termins, gespeichert als ISO-8601-String in UTC
    /// (z. B. "2026-07-12T17:00:00.000Z"). Wir speichern hier bewusst KEIN
    /// DateTime-Objekt, weil SQLite selbst keinen Datums-Typ kennt - ein
    /// UTC-ISO-String lässt sich problemlos sortieren und zwischen den
    /// Plattformen (Android/iOS/Windows) verlustfrei austauschen.
    /// Für die Anzeige wird der String über den <see cref="Converters.IsoToDisplayDateConverter"/>
    /// wieder in ein lesbares Datum/Uhrzeit-Format umgewandelt.
    /// </summary>
    [Indexed]
    [NotNull]
    [Column("date_time")]
    public string ScheduledAt { get; set; } = DateTimeHelper.UtcNowIsoString();

    /// <summary>
    /// Foreign Key auf den gewählten Ort (locations-Tabelle). Darf NULL sein,
    /// falls beim Anlegen des Termins kein Ort ausgewählt wurde.
    ///
    /// Wichtig: Das ist eine echte Fremdschlüssel-Spalte! Hier darf nur die Id
    /// eines existierenden locations-Datensatzes stehen, kein Freitext wie
    /// "Bei Anna" - sonst wirft SQLite beim Speichern einen
    /// "FOREIGN KEY constraint failed"-Fehler (siehe Popup: dort wird der
    /// Ort deshalb über einen Picker aus der DB ausgewählt, nicht getippt).
    /// </summary>
    [Indexed]
    [Column("location_id")]
    public string? LocationId { get; set; }

    /// <summary>
    /// Foreign Key auf den Veranstalter/Gastgeber (players-Tabelle). Genau wie
    /// LocationId eine echte FK-Spalte und daher ebenfalls optional (null = kein
    /// Veranstalter gewählt) statt Freitext.
    /// </summary>
    [Indexed]
    [Column("host_player_id")]
    public string? HostPlayerId { get; set; }

    /// <summary>
    /// Status des Termins: "planned", "cancelled" oder "completed"
    /// (siehe <see cref="BoardGamerConstants.GameNightStatus"/>). In der Datenbank
    /// gibt es dafür einen CHECK-Constraint, der nur genau diese drei Werte erlaubt.
    /// </summary>
    [NotNull]
    [Column("status")]
    public string Status { get; set; } = BoardGamerConstants.GameNightStatus.Planned;

    /// <summary>
    /// Freie Notiz zum Termin (z. B. "Bitte Snacks mitbringen"). Anders als
    /// Ort/Veranstalter/Spiel ist das reiner Freitext ohne Verknüpfung zu einer
    /// anderen Tabelle - deshalb hier auch okay, dass der Nutzer frei tippen darf.
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    // ---------------------------------------------------------------------
    // Die folgenden drei Properties sind KEINE echten Datenbankspalten
    // ([Ignore] sagt SQLite-net: "nicht in die Tabelle schreiben/lesen").
    // Sie dienen nur der Anzeige in der UI und werden von EventViewModel
    // nach dem Laden der Termine befüllt, indem LocationId/HostPlayerId
    // gegen die locations-/players-Tabelle nachgeschlagen werden
    // (siehe EventViewModel.ApplyDisplayNames). GameName kommt sogar aus
    // einer dritten Tabelle (game_suggestions), weil game_nights selbst
    // gar keine game_id-Spalte besitzt.
    //
    // Warum nicht einfach den Namen direkt in der Datenbank speichern?
    // Weil sich z. B. der Name eines Orts später ändern könnte (Umzug,
    // Tippfehler-Korrektur) - dann müsste man ihn in JEDEM Termin
    // nachträglich anpassen. Über die Foreign-Key-Id bleibt der Termin
    // immer automatisch mit dem aktuellen Namen verknüpft.
    // ---------------------------------------------------------------------

    /// <summary>Anzeigename der Spielgruppe (aufgelöst aus GroupId, z. B. "Mittwochsrunde"). Nicht in der DB gespeichert.</summary>
    [Ignore]
    public string? GroupName { get; set; }

    /// <summary>Anzeigename des Orts (aufgelöst aus LocationId). Nicht in der DB gespeichert.</summary>
    [Ignore]
    public string? LocationName { get; set; }

    /// <summary>Anzeigename des Veranstalters (aufgelöst aus HostPlayerId). Nicht in der DB gespeichert.</summary>
    [Ignore]
    public string? HostName { get; set; }

    /// <summary>
    /// Anzeigename des/der vorgeschlagenen Spiele (aufgelöst über die game_suggestions-Tabelle).
    /// Enthält mehrere Titel, kommagetrennt, falls für einen Termin mehrere Spiele vorgeschlagen wurden.
    /// Nicht in der DB gespeichert.
    /// </summary>
    [Ignore]
    public string? GameName { get; set; }

    // ---------------------------------------------------------------------
    // Die folgenden drei Properties gehören zum Bewertungs-Feature
    // (Views/RatingPage.xaml, ViewModels/PreviousEventsViewModel.cs). Auch sie
    // sind [Ignore] - also nicht in der DB gespeichert, sondern werden von
    // PreviousEventsViewModel.InitializeAsync() befüllt, indem die Tabelle
    // game_night_reviews mit dem aktuell angemeldeten Spieler (siehe
    // CurrentPlayerService) abgeglichen wird. Sie sagen der UI, WAS beim
    // Antippen dieses Termins auf PreviousEventsPage passieren soll.
    // ---------------------------------------------------------------------

    /// <summary>
    /// True, wenn der AKTUELLE Spieler (CurrentPlayerService.PlayerId) der Gastgeber
    /// (HostPlayerId) dieses Termins ist. Wird sowohl von EventViewModel (für die
    /// Zusagen/Absagen-Logik: ein Gastgeber muss seinem eigenen Termin nicht zusagen)
    /// als auch von PreviousEventsViewModel (Bewertungs-Feature: ein Gastgeber kann
    /// seinen eigenen Abend nicht bewerten, aber sehen, wie er bewertet wurde) gesetzt.
    /// </summary>
    [Ignore]
    public bool IsHostedByCurrentPlayer { get; set; }

    /// <summary>
    /// True, wenn der aktuelle Spieler für diesen Termin bereits einen Eintrag in
    /// game_night_reviews angelegt hat. Eine zweite Bewertung ist nicht vorgesehen
    /// (siehe UNIQUE-Constraint auf game_night_id+reviewer_player_id in der DB).
    /// </summary>
    [Ignore]
    public bool IsRatedByCurrentPlayer { get; set; }

    /// <summary>
    /// True, wenn der aktuelle Spieler diesen Termin JETZT bewerten darf: der Termin
    /// muss abgeschlossen sein (Status "completed"), der Spieler darf nicht der
    /// Gastgeber sein und darf noch keine Bewertung abgegeben haben.
    /// </summary>
    [Ignore]
    public bool CanBeRatedByCurrentPlayer { get; set; }

    /// <summary>
    /// Text für das Badge auf PreviousEventsPage ("Bewerten", "Bewertet" oder
    /// "Deine Bewertung"). Null/leer blendet das Badge komplett aus (z. B. wenn der
    /// Termin aus irgendeinem Grund noch nicht "completed" ist).
    /// </summary>
    [Ignore]
    public string? RatingBadgeText { get; set; }

    /// <summary>
    /// True für genau den einen Termin, der von allen zukünftigen Terminen als
    /// nächstes ansteht (chronologisch der früheste in EventViewModel.UpcomingGameNights).
    /// Wird von EventViewModel.RecomputeNextUpcoming() gesetzt und auf MainPage benutzt,
    /// um diesen einen Termin optisch hervorzuheben (siehe MainPage.xaml). Nicht in der
    /// DB gespeichert.
    /// </summary>
    [Ignore]
    public bool IsNextUpcoming { get; set; }

    // ---------------------------------------------------------------------
    // Die folgenden vier Properties gehören zur Zusagen/Absagen-Logik
    // (EventViewModel.ApplyAttendanceInfoAsync, Views/MainPage.xaml, Views/EventPage.xaml).
    // Sie werten die Tabelle "attendance" für diesen Termin aus und sind, wie die
    // anderen Anzeige-Properties hier, [Ignore] - also nicht in der DB gespeichert.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Der Antwort-Status ("accepted"/"declined", siehe BoardGamerConstants.AttendanceStatus)
    /// des AKTUELLEN Spielers für diesen Termin, oder null, falls noch keine Antwort
    /// in der Tabelle attendance existiert.
    /// </summary>
    [Ignore]
    public string? MyAttendanceStatus { get; set; }

    /// <summary>
    /// Lesbarer Anzeigetext zu <see cref="MyAttendanceStatus"/> (z. B. "Du hast zugesagt"),
    /// oder null, solange der aktuelle Spieler noch nicht geantwortet hat.
    /// </summary>
    [Ignore]
    public string? MyAttendanceStatusText { get; set; }

    /// <summary>
    /// True, wenn der aktuelle Spieler für diesen Termin über "Zusagen"/"Absagen"
    /// antworten darf: der Termin muss noch "planned" sein, und der Spieler darf nicht
    /// selbst der Gastgeber sein (der Gastgeber nimmt automatisch teil).
    /// </summary>
    [Ignore]
    public bool CanRespondToAttendance { get; set; }

    /// <summary>
    /// Anzeigetext für den Anteil ALLER aktiven Gruppenmitglieder (inklusive Gastgeber,
    /// der automatisch als "zugesagt" zählt), die bereits zugesagt haben, z. B.
    /// "100% zugesagt (3/3)". Null, wenn es keine aktiven Gruppenmitglieder gibt (dann
    /// lässt sich kein Anteil bilden).
    /// </summary>
    [Ignore]
    public string? AttendanceSummaryText { get; set; }

    /// <summary>
    /// True, wenn dieser Termin abgesagt ist (Status "cancelled") - egal ob durch die
    /// automatische Mehrheits-Absage-Regel (siehe EventViewModel.ApplyAttendanceInfoAsync)
    /// oder durch manuelles Löschen (siehe GameNightRepository.SoftDeleteAsync). Rein aus
    /// Status abgeleitet, deshalb ohne eigenen Setter und ohne Zutun eines ViewModels
    /// immer aktuell.
    /// </summary>
    [Ignore]
    public bool IsCancelled => Status == BoardGamerConstants.GameNightStatus.Cancelled;

    /// <summary>
    /// True, wenn der aktuelle Spieler diesen Termin bearbeiten darf: nur der Gastgeber
    /// (Ersteller) selbst, und nur solange der Termin nicht abgesagt ist - ein abgesagter
    /// Termin bleibt unveränderlich, auch für den Gastgeber. Andere Gruppenmitglieder
    /// können höchstens über CanRespondToAttendance ihre Zu-/Absage ändern, aber nicht
    /// den Termin selbst bearbeiten.
    /// </summary>
    [Ignore]
    public bool CanBeEditedByCurrentPlayer => IsHostedByCurrentPlayer && !IsCancelled;

    /// <summary>
    /// True, wenn der aktuelle Spieler diesen Termin über den "Termin absagen"-Button
    /// komplett canceln darf: nur der Gastgeber selbst, und nur solange der Termin noch
    /// "planned" ist (ein bereits abgesagter oder bereits stattgefundener/"completed"
    /// Termin lässt sich nicht nochmal absagen).
    /// </summary>
    [Ignore]
    public bool CanCancelEventByHost =>
        IsHostedByCurrentPlayer && Status == BoardGamerConstants.GameNightStatus.Planned;

    /// <summary>
    /// True, wenn der "Vorschläge"-Button (Spielvorschläge/Abstimmen, siehe
    /// EventViewModel.OpenSuggestionsAsync) für diesen Termin angezeigt werden soll -
    /// bei einem bereits abgesagten Termin ergibt eine weitere Abstimmung über die
    /// Spiele keinen Sinn mehr, deshalb hier ausgeblendet.
    /// </summary>
    [Ignore]
    public bool CanOpenSuggestions => !IsCancelled;

    // ---------------------------------------------------------------------
    // Die folgenden vier Properties gehören zum Spielvorschläge/Abstimmen-Feature
    // (EventViewModel.ApplyTopVotedGame, Views/MainPage.xaml, Views/EventPage.xaml,
    // Views/GameNightSuggestionsPage.xaml). Sie werten die Tabellen "game_suggestions"
    // und "game_votes" für diesen Termin aus und sind, wie die anderen
    // Anzeige-Properties hier, [Ignore] - also nicht in der DB gespeichert.
    // ---------------------------------------------------------------------

    [Ignore]
    public string? TopVotedGameName { get; set; }

    [Ignore]
    public int TopVotedGameVoteCount { get; set; }

    [Ignore]
    public bool HasTopVotedGame =>
        !string.IsNullOrWhiteSpace(TopVotedGameName) && TopVotedGameVoteCount > 0;

    [Ignore]
    public string TopVotedGameDisplayText =>
        HasTopVotedGame
            ? $"Favorit: {TopVotedGameName} ({TopVotedGameVoteCount} {(TopVotedGameVoteCount == 1 ? "Stimme" : "Stimmen")})"
            : string.Empty;
}
