namespace BoardGamerApp.Services;

public class GroupDelayMessageService
{
    private readonly MessageApiClient _messageApiClient;

    public GroupDelayMessageService(MessageApiClient messageApiClient)
    {
        _messageApiClient = messageApiClient;
    }

    public async Task SendDelayMessageToGroupAsync(
        string groupId,
        string currentPlayerId,
        int delayMinutes)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new InvalidOperationException("Es wurde keine Gruppe gefunden.");

        if (string.IsNullOrWhiteSpace(currentPlayerId))
            throw new InvalidOperationException("Es wurde kein aktueller Spieler gefunden.");

        var response = await _messageApiClient.SendDelayMessageAsync(
            new DelayMessageRequest
            {
                GroupId = groupId,
                SenderPlayerId = currentPlayerId,
                DelayMinutes = delayMinutes
            });

        await Shell.Current.DisplayAlertAsync(
            "Nachricht gesendet",
            $"Die Verspätungsnachricht wurde an {response.RecipientCount} Spieler:innen gesendet.",
            "OK");
    }
}