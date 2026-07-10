using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BoardGamerApp.Messages;

public class GroupMembersChangedMessage : ValueChangedMessage<string>
{
    public GroupMembersChangedMessage(string groupId)
        : base(groupId)
    {
    }
}
