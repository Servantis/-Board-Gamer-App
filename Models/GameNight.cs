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
    // Die folgenden vier Properties gehören zum Bewertungs-Feature
    // (Views/RatingPage.xaml, ViewModels/PreviousEventsViewModel.cs). Auch sie
    // sind [Ignore] - also nicht in der DB gespeichert, sondern werden von
    // PreviousEventsViewModel.InitializeAsync() befüllt, indem die Tabelle
    // game_night_reviews mit dem aktuell angemeldeten Spieler (siehe
    // CurrentPlayerService) abgeglichen wird. Sie sagen der UI, WAS beim
    // Antippen dieses Termins auf PreviousEventsPage passieren soll.
    // ---------------------------------------------------------------------

    /// <summary>
    /// True, wenn der AKTUELLE Spieler (CurrentPlayerService.PlayerId) der Gastgeber
    /// (HostPlayerId) dieses Termins war. Ein Gastgeber soll seinen eigenen Abend
    /// nicht bewerten können - er darf aber sehen, wie er bewertet wurde.
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
}
