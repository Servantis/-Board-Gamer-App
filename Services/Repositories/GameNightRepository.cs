using System.Text.Json;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using SQLite;

namespace BoardGamerApp.Repositories;

/// <summary>
/// Das "Repository" ist die einzige Klasse, die direkt mit der Tabelle
/// <c>game_nights</c> spricht. Alles andere in der App (ViewModels, Views)
/// greift NICHT direkt auf SQLite zu, sondern nur über diese Klasse.
///
/// Warum dieser Umweg? Zwei Gründe:
/// 1. Trennung von Verantwortlichkeiten (Separation of Concerns): Das ViewModel
///    kümmert sich um die UI-Logik, das Repository um "wie kommt der Termin
///    in/aus der Datenbank". Ändert sich später z. B. die Query, muss nur
///    hier etwas angepasst werden - nicht überall dort, wo Termine gebraucht werden.
/// 2. Testbarkeit: Man könnte in einem Unit-Test ein "Fake"-Repository
///    einsetzen, ohne eine echte Datenbank zu brauchen.
///
/// Dieses Repository bekommt den <see cref="DatabaseService"/> per Konstruktor-Injection
/// (Dependency Injection, registriert in MauiProgram.cs). Dadurch teilen sich
/// alle Repositories dieselbe, einmal geöffnete SQLite-Verbindung.
/// </summary>
public class GameNightRepository
{
    private readonly DatabaseService _database;

    public GameNightRepository(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Lädt alle nicht gelöschten Termine, sortiert nach Datum.
    /// "Nicht gelöscht" heißt: DeletedAt == null. Diese App löscht nämlich nie
    /// wirklich einen Datensatz aus der Tabelle (Hard Delete), sondern setzt nur
    /// das DeletedAt-Feld (Soft Delete) - siehe <see cref="SoftDeleteAsync"/>.
    /// Das ist üblich, wenn man später mit einem Server synchronisieren will:
    /// so "weiß" auch der Server, dass ein Termin gelöscht wurde, statt dass
    /// er einfach verschwindet.
    /// </summary>
    public async Task<List<GameNight>> GetAllAsync()
    {
        var db = await _database.GetConnectionAsync();

        return await db.Table<GameNight>()
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync();
    }

    /// <summary>Lädt alle Termine einer bestimmten Spielgruppe (siehe GameNight.GroupId).</summary>
    public async Task<List<GameNight>> GetByGroupAsync(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Die Gruppen-ID darf nicht leer sein.", nameof(groupId));

        var db = await _database.GetConnectionAsync();

        return await db.Table<GameNight>()
            .Where(x => x.GroupId == groupId && x.DeletedAt == null)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync();
    }

    /// <summary>Sucht einen einzelnen Termin über seine Id.</summary>
    public async Task<GameNight?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Die Termin-ID darf nicht leer sein.", nameof(id));

        var db = await _database.GetConnectionAsync();

        return await db.Table<GameNight>()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Legt einen neuen Termin in der Datenbank an.
    /// Vergibt bei Bedarf eine neue Id und setzt CreatedAt/UpdatedAt/Version -
    /// darum muss sich der Aufrufer (EventViewModel) nicht kümmern.
    /// </summary>
    public async Task AddAsync(GameNight night)
    {
        ValidateGameNight(night);

        var db = await _database.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        if (string.IsNullOrWhiteSpace(night.Id))
            night.Id = Guid.NewGuid().ToString();

        night.CreatedAt = now;
        night.UpdatedAt = now;
        night.DeletedAt = null;
        night.Version = 1;

        await db.InsertAsync(night);

        // Zusätzlich zum eigentlichen Insert wird ein Eintrag in die "Sync Outbox"
        // geschrieben (siehe AddToSyncOutboxAsync weiter unten) - das ist eine
        // Warteschlange für Änderungen, die später mit einem Server abgeglichen
        // werden sollen. Für die reine Offline-Nutzung der App ist das nicht
        // zwingend nötig, gehört aber zum Sync-Konzept dieses Projekts dazu.
        await AddToSyncOutboxAsync(db, night, BoardGamerConstants.SyncOperations.Insert);
    }

    /// <summary>Aktualisiert einen bestehenden Termin (erhöht automatisch die Version).</summary>
    public async Task UpdateAsync(GameNight night)
    {
        ValidateGameNight(night);

        var db = await _database.GetConnectionAsync();

        night.UpdatedAt = DateTimeHelper.UtcNowIsoString();
        night.Version += 1;

        await db.UpdateAsync(night);

        await AddToSyncOutboxAsync(db, night, BoardGamerConstants.SyncOperations.Update);
    }

    /// <summary>
    /// "Löscht" einen Termin, ohne die Zeile wirklich aus der Tabelle zu entfernen -
    /// es wird nur DeletedAt gesetzt. Dadurch tauchen gelöschte Termine in
    /// GetAllAsync/GetByGroupAsync nicht mehr auf, bleiben aber technisch erhalten.
    /// </summary>
    public async Task SoftDeleteAsync(GameNight night)
    {
        var db = await _database.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        night.DeletedAt = now;
        night.UpdatedAt = now;
        night.Version += 1;

        await db.UpdateAsync(night);

        await AddToSyncOutboxAsync(db, night, BoardGamerConstants.SyncOperations.Delete);
    }

    /// <summary>
    /// Einfache Plausibilitätsprüfung, bevor ein Termin gespeichert wird.
    /// Das ersetzt keine Datenbank-Constraints (die gelten immer), sondern
    /// gibt uns schon in C# eine verständliche Fehlermeldung, bevor SQLite
    /// mit einem kryptischeren Fehler abbrechen würde.
    /// </summary>
    private static void ValidateGameNight(GameNight night)
    {
        if (string.IsNullOrWhiteSpace(night.GroupId))
            throw new InvalidOperationException("Der Termin muss einer Gruppe zugeordnet sein.");

        if (string.IsNullOrWhiteSpace(night.ScheduledAt))
            throw new InvalidOperationException("Der Termin benötigt ein Datum/Uhrzeit.");
    }

    /// <summary>
    /// Schreibt einen Eintrag in die sync_outbox-Tabelle. Das ist quasi ein
    /// "Änderungsprotokoll": jede Insert/Update/Delete-Operation wird hier als
    /// JSON zwischengespeichert, damit ein (aktuell noch nicht gebauter) Sync-Dienst
    /// diese Änderungen später an einen Server schicken könnte.
    /// </summary>
    private static async Task AddToSyncOutboxAsync(
        SQLiteAsyncConnection database,
        GameNight night,
        string operation)
    {
        var outboxEntry = new SyncOutboxEntry
        {
            Id = Guid.NewGuid().ToString(),
            EntityName = "game_nights",
            EntityId = night.Id,
            Operation = operation,
            PayloadJson = BuildPayloadJson(night),
            CreatedAt = DateTimeHelper.UtcNowIsoString(),
            RetryCount = 0,
            LastError = null
        };

        await database.InsertAsync(outboxEntry);
    }

    /// <summary>Baut den JSON-Snapshot des Termins, der in der Sync Outbox gespeichert wird.</summary>
    private static string BuildPayloadJson(GameNight night)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = night.Id,
            ["group_id"] = night.GroupId,
            ["date_time"] = night.ScheduledAt,
            ["location_id"] = night.LocationId,
            ["host_player_id"] = night.HostPlayerId,
            ["status"] = night.Status,
            ["notes"] = night.Notes,
            ["created_at"] = night.CreatedAt,
            ["updated_at"] = night.UpdatedAt,
            ["deleted_at"] = night.DeletedAt,
            ["version"] = night.Version
        };

        return JsonSerializer.Serialize(payload);
    }
}
