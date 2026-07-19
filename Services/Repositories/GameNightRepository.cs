using System.Text.Json;
using BoardGamerApp.Models;
using BoardGamerApp.Services;
using SQLite;
using System.Diagnostics;

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
    ///
    /// Zusätzlich wird der Status auf "cancelled" gesetzt (siehe
    /// BoardGamerConstants.GameNightStatus). Das ist rein fachlich sinnvoll: ein
    /// gelöschter Termin wurde ja abgesagt, nicht "erledigt" oder weiterhin "geplant" -
    /// und falls DeletedAt aus irgendeinem Grund mal ignoriert würde (z. B. in einem
    /// späteren Server-Sync), zeigt der Status trotzdem korrekt an, dass der Termin
    /// storniert ist.
    /// </summary>
    public async Task SoftDeleteAsync(GameNight night)
    {
        var db = await _database.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        night.DeletedAt = now;
        night.UpdatedAt = now;
        night.Status = BoardGamerConstants.GameNightStatus.Cancelled;
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

    // Liefert den nächsten geplanten Termin einer Spielgruppe zurück (Status = "planned", sortiert nach Datum, nur der erste).
    public async Task<GameNight?> GetNextPlannedGameNightAsync(string groupId)
    {
        var db = await _database.GetConnectionAsync();

        const string sql = """
        SELECT *
        FROM game_nights
        WHERE group_id = ?
          AND status = 'planned'
          AND deleted_at IS NULL
        ORDER BY date_time
        LIMIT 1;
        """;

        var result = await db.QueryAsync<GameNight>(sql, groupId);

        return result.FirstOrDefault();
    }

    // Setzt den Host-Spieler für einen Termin.
    public async Task AssignHostAsync(
    string gameNightId,
    string playerId)
    {
        var db = await _database.GetConnectionAsync();

        var now = DateTimeHelper.UtcNowIsoString();

        // Debug.WriteLine($"[HOST] Host {playerId} wurde GameNight {gameNightId} zugewiesen.");

        const string sql = """
        UPDATE game_nights
        SET
            host_player_id = ?,
            updated_at = ?,
            version = version + 1
        WHERE id = ?
          AND deleted_at IS NULL;
        """;

/*
        Debug.WriteLine(
            $"[GAME NIGHT] Schreibe Host in GameNight => " +
            $"GameNightId={gameNightId} | " +
            $"PlayerId={playerId}");
*/
        await db.ExecuteAsync(
            sql,
            playerId,
            now,
            gameNightId);


        var verify = await db.QueryAsync<GameNight>(
            """
    SELECT *
    FROM game_nights
    WHERE id = ?
    """,
            gameNightId);

        var savedNight = verify.FirstOrDefault();
/*
        Debug.WriteLine(
            $"[GAME NIGHT VERIFY] => " +
            $"Night={savedNight?.Id} | " +
            $"HostPlayerId={savedNight?.HostPlayerId}");
*/
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

    // Liefert nur Mitglieder, die im aktuellen Zyklus bereits Gasteber waren.
    // Zudem wird das Datum der vergangenen Termine mitgegeben
    public async Task<List<LastHostItem>>
    GetLastHostsAsync(string groupId)
    {
        var db = await _database.GetConnectionAsync();

        const string sql = """
    SELECT
        gm.player_id AS PlayerId,
        p.name AS PlayerName,
        (
            SELECT MAX(gn.date_time)
            FROM game_nights gn
            WHERE gn.host_player_id = gm.player_id
              AND gn.group_id = gm.group_id
              AND gn.status = 'completed'
              AND gn.deleted_at IS NULL
        ) AS HostedDate

    FROM group_members gm

    INNER JOIN players p
        ON p.id = gm.player_id

    WHERE gm.group_id = ?
      AND gm.hosted_flag = 1
      AND gm.deleted_at IS NULL

    ORDER BY HostedDate DESC;
    """;

        //Test
        var rows = await db.QueryAsync<GameNight>(
            """
    SELECT *
    FROM game_nights
    WHERE host_player_id = ?
    ORDER BY date_time DESC
    """,
            "player-tom-001");

        foreach (var row in rows)
        {
            /*
            Debug.WriteLine(
                $"GAME NIGHT => " +
                $"Date={row.ScheduledAt} | " +
                $"Status={row.Status}");
            */
        }

        return await db.QueryAsync<LastHostItem>(
            sql,
            groupId);
    }

    // / Liefert die PlayerId des letzten abgeschlossenen Hosts einer Spielgruppe zurück.
    public async Task<string?> GetLastCompletedHostPlayerIdAsync(
        string groupId)
    {
        var db = await _database.GetConnectionAsync();

        const string sql = """
    SELECT host_player_id
    FROM game_nights
    WHERE group_id = ?
      AND status = 'completed'
      AND deleted_at IS NULL
      AND host_player_id IS NOT NULL
    ORDER BY date_time DESC
    LIMIT 1;
    """;

        var result = await db.ExecuteScalarAsync<string>(
            sql,
            groupId);

        return result;
    }

}
