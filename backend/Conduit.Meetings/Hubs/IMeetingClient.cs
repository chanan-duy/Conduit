using Conduit.Meetings.Contracts;

namespace Conduit.Meetings.Hubs;

public interface IMeetingClient
{
	Task ReceiveChatMessage(ChatMessageDto message);
}
