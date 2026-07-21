using System.Text.Json.Serialization;

namespace BoardGamerApp.Services;

public class SyncPushRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("changes")]
    public List<SyncPushChange> Changes { get; set; } = new();
}

public class SyncPushChange
{
    [JsonPropertyName("outbox_id")]
    public string OutboxId { get; set; } = string.Empty;

    [JsonPropertyName("entity_name")]
    public string EntityName { get; set; } = string.Empty;

    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("payload_json")]
    public string PayloadJson { get; set; } = string.Empty;
}

public class SyncPushResponse
{
    [JsonPropertyName("received_count")]
    public int ReceivedCount { get; set; }

    [JsonPropertyName("accepted_count")]
    public int AcceptedCount { get; set; }

    [JsonPropertyName("rejected_count")]
    public int RejectedCount { get; set; }

    [JsonPropertyName("server_time")]
    public string? ServerTime { get; set; }

    [JsonPropertyName("accepted")]
    public List<SyncResultEntry> Accepted { get; set; } = new();

    [JsonPropertyName("rejected")]
    public List<SyncResultEntry> Rejected { get; set; } = new();
}

public class SyncResultEntry
{
    [JsonPropertyName("outbox_id")]
    public string? OutboxId { get; set; }

    [JsonPropertyName("entity_name")]
    public string? EntityName { get; set; }

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    [JsonPropertyName("operation")]
    public string? Operation { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class OutboxPushResult
{
    public int LocalPendingCount { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class SyncPullResponse
{
    [JsonPropertyName("server_time")]
    public string? ServerTime { get; set; }

    [JsonPropertyName("since")]
    public string? Since { get; set; }

    [JsonPropertyName("change_count")]
    public int ChangeCount { get; set; }

    [JsonPropertyName("changes")]
    public List<SyncPullChange> Changes { get; set; } = new();
}

public class SyncPullChange
{
    [JsonPropertyName("change_id")]
    public string ChangeId { get; set; } = string.Empty;

    [JsonPropertyName("entity_name")]
    public string EntityName { get; set; } = string.Empty;

    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("payload_json")]
    public string PayloadJson { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class SyncPullResult
{
    public int ReceivedCount { get; set; }

    public int AppliedCount { get; set; }

    public int FailedCount { get; set; }

    public string Message { get; set; } = string.Empty;
}