using System.Text.Json.Serialization;

namespace BoardGamerApp.Services;

public class DelayMessageRequest
{
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [JsonPropertyName("sender_player_id")]
    public string SenderPlayerId { get; set; } = string.Empty;

    [JsonPropertyName("delay_minutes")]
    public int DelayMinutes { get; set; }
}

public class DelayMessageResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("recipient_count")]
    public int RecipientCount { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}