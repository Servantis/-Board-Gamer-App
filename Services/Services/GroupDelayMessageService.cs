using BoardGamerApp.Repositories;

namespace BoardGamerApp.Services;

public class GroupDelayMessageService
{
    private readonly GroupMessageRepository _groupMessageRepository;
    private readonly GroupEmailService _groupEmailService;

    public GroupDelayMessageService(
        GroupMessageRepository groupMessageRepository,
        GroupEmailService groupEmailService)
    {
        _groupMessageRepository = groupMessageRepository;
        _groupEmailService = groupEmailService;
    }

    public async Task ComposeDelayMessageToGroupAsync(
        string groupId,
        string currentPlayerId,
        string currentPlayerName,
        int delayMinutes,
        string? customMessage = null)
    {
        var recipients = await _groupMessageRepository.GetActiveGroupRecipientsAsync(
            groupId,
            currentPlayerId);

        var emailAddresses = recipients
            .Select(recipient => recipient.Email)
            .ToList();

        await _groupEmailService.ComposeDelayMessageAsync(
            emailAddresses,
            currentPlayerName,
            delayMinutes,
            customMessage);
    }
}
