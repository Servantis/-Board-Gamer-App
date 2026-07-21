using BoardGamerApp.Models;
using System.Text.Json;

namespace BoardGamerApp.Services;

public class AppSyncService
{
    private readonly DatabaseService _databaseService;
    private readonly SyncApiClient _syncApiClient;
    private readonly DeviceIdentityService _deviceIdentityService;

    public AppSyncService(
        DatabaseService databaseService,
        SyncApiClient syncApiClient,
        DeviceIdentityService deviceIdentityService)
    {
        _databaseService = databaseService;
        _syncApiClient = syncApiClient;
        _deviceIdentityService = deviceIdentityService;
    }

    private static readonly string[] BootstrapTableOrder =
{
    "players",
    "player_devices",
    "gaming_groups",
    "group_members",
    "locations",
    "games",
    "game_nights",
    "attendance",
    "game_suggestions",
    "game_votes",
    "game_night_reviews"
};

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        WriteIndented = false
    };


    public async Task<OutboxPushResult> PushPendingOutboxAsync(int limit = 100)
    {
        var pendingEntries = await _databaseService.GetPendingSyncOutboxEntriesAsync(limit);

        if (pendingEntries.Count == 0)
        {
            return new OutboxPushResult
            {
                LocalPendingCount = 0,
                AcceptedCount = 0,
                RejectedCount = 0,
                Message = "Keine offenen Sync-Outbox-Einträge vorhanden."
            };
        }

        var request = new SyncPushRequest
        {
            DeviceId = _deviceIdentityService.GetInstallationId(),
            Changes = pendingEntries.Select(entry => new SyncPushChange
            {
                OutboxId = entry.Id,
                EntityName = entry.EntityName,
                EntityId = entry.EntityId,
                Operation = entry.Operation,
                PayloadJson = entry.PayloadJson
            }).ToList()
        };

        var response = await _syncApiClient.PushAsync(request);

        foreach (var acceptedEntry in response.Accepted)
        {
            if (string.IsNullOrWhiteSpace(acceptedEntry.OutboxId))
                continue;

            await _databaseService.DeleteSyncOutboxEntryAsync(acceptedEntry.OutboxId);
        }

        foreach (var rejectedEntry in response.Rejected)
        {
            if (string.IsNullOrWhiteSpace(rejectedEntry.OutboxId))
                continue;

            var reason = rejectedEntry.Reason ?? "Unbekannter Sync-Fehler.";

            await _databaseService.MarkSyncOutboxEntryFailedAsync(
                rejectedEntry.OutboxId,
                reason
            );
        }

        var syncState = await _databaseService.GetSyncStateAsync();
        syncState.LastPushAt = response.ServerTime ?? DateTimeHelper.UtcNowIsoString();
        await _databaseService.UpdateSyncStateAsync(syncState);

        return new OutboxPushResult
        {
            LocalPendingCount = pendingEntries.Count,
            AcceptedCount = response.AcceptedCount,
            RejectedCount = response.RejectedCount,
            Message =
                $"Sync abgeschlossen. Lokal offen: {pendingEntries.Count}, " +
                $"angenommen: {response.AcceptedCount}, " +
                $"abgelehnt: {response.RejectedCount}."
        };
    }

    public async Task<OutboxPushResult> PushInitialSnapshotAsync()
    {
        var changes = new List<SyncPushChange>();

        foreach (var tableName in BootstrapTableOrder)
        {
            var rows = await _databaseService.GetRowsForSyncAsync(tableName);

            foreach (var row in rows)
            {
                if (!row.TryGetValue("id", out var idValue) || idValue is null)
                    continue;

                var entityId = idValue.ToString();

                if (string.IsNullOrWhiteSpace(entityId))
                    continue;

                changes.Add(new SyncPushChange
                {
                    OutboxId = $"bootstrap-{tableName}-{entityId}",
                    EntityName = tableName,
                    EntityId = entityId,
                    Operation = "INSERT",
                    PayloadJson = JsonSerializer.Serialize(row, PayloadJsonOptions)
                });
            }
        }

        if (changes.Count == 0)
        {
            return new OutboxPushResult
            {
                LocalPendingCount = 0,
                AcceptedCount = 0,
                RejectedCount = 0,
                Message = "Keine Initialdaten zum Pushen gefunden."
            };
        }

        var request = new SyncPushRequest
        {
            DeviceId = _deviceIdentityService.GetInstallationId(),
            Changes = changes
        };

        var response = await _syncApiClient.PushAsync(request);

        return new OutboxPushResult
        {
            LocalPendingCount = changes.Count,
            AcceptedCount = response.AcceptedCount,
            RejectedCount = response.RejectedCount,
            Message =
                $"Initialdaten-Sync abgeschlossen. " +
                $"Gesendet: {changes.Count}, " +
                $"angenommen: {response.AcceptedCount}, " +
                $"abgelehnt: {response.RejectedCount}."
        };
    }

    public async Task<SyncPullResult> PullServerChangesAsync()
    {
        var syncState = await _databaseService.GetSyncStateAsync();

        var response = await _syncApiClient.PullAsync(syncState.LastPullAt);

        var appliedCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        foreach (var change in response.Changes)
        {
            try
            {
                await _databaseService.ApplyRemoteChangeAsync(
                    change.EntityName,
                    change.EntityId,
                    change.Operation,
                    change.PayloadJson
                );

                appliedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;

                errors.Add(
                    $"{change.EntityName}/{change.EntityId}: {ex.Message}"
                );
            }
        }

        if (failedCount == 0)
        {
            syncState.LastPullAt = response.ServerTime ?? DateTimeHelper.UtcNowIsoString();
            await _databaseService.UpdateSyncStateAsync(syncState);
        }

        var message =
            $"Pull abgeschlossen. " +
            $"Empfangen: {response.ChangeCount}, " +
            $"angewendet: {appliedCount}, " +
            $"fehlgeschlagen: {failedCount}.";

        if (failedCount > 0)
        {
            message += "\n\nErster Fehler:\n" + errors.FirstOrDefault();
            message += "\n\nlast_pull_at wurde nicht aktualisiert, damit der Pull erneut versucht werden kann.";
        }

        return new SyncPullResult
        {
            ReceivedCount = response.ChangeCount,
            AppliedCount = appliedCount,
            FailedCount = failedCount,
            Message = message
        };
    }


}