using Livekit.Server.Sdk.Dotnet;

namespace Conduit.Meetings.Services;

public class LiveKitRooms
{
	private readonly string _apiKey;
	private readonly string _apiSecret;
	private readonly RoomServiceClient _rooms;

	public string PublicUrl { get; }

	public LiveKitRooms(IConfiguration config)
	{
		_apiKey = config["LiveKit:ApiKey"] ?? throw new InvalidOperationException("'LiveKit:ApiKey' is not configured.");
		_apiSecret = config["LiveKit:ApiSecret"] ?? throw new InvalidOperationException("'LiveKit:ApiSecret' is not configured.");
		PublicUrl = config["LiveKit:PublicUrl"] ?? throw new InvalidOperationException("'LiveKit:PublicUrl' is not configured.");
		var apiUrl = config["LiveKit:ApiUrl"] ?? throw new InvalidOperationException("'LiveKit:ApiUrl' is not configured.");

		_rooms = new RoomServiceClient(apiUrl, _apiKey, _apiSecret);
	}

	public string CreateJoinToken(Guid meetingId, Guid userId, string userName)
	{
		return new AccessToken(_apiKey, _apiSecret)
			.WithRoomConfig(new RoomConfiguration { DepartureTimeout = 10 })
			.WithIdentity(userId.ToString())
			.WithName(userName)
			.WithGrants(new VideoGrants
			{
				RoomJoin = true,
				Room = meetingId.ToString(),
				CanPublish = true,
				CanSubscribe = true,
				CanPublishData = false,
			})
			.WithTtl(TimeSpan.FromMinutes(5))
			.ToJwt();
	}

	public async Task RemoveParticipantAsync(Guid meetingId, Guid userId)
	{
		var name = meetingId.ToString();
		var participants = await _rooms.ListParticipants(new ListParticipantsRequest { Room = name });
		if (participants.Participants.Any(p => p.Identity == userId.ToString()))
		{
			await _rooms.RemoveParticipant(new RoomParticipantIdentity { Room = name, Identity = userId.ToString() });
		}
	}

	public async Task CloseRoomAsync(Guid meetingId)
	{
		var name = meetingId.ToString();
		var rooms = await _rooms.ListRooms(new ListRoomsRequest { Names = { name } });
		if (rooms.Rooms.Count > 0)
		{
			await _rooms.DeleteRoom(new DeleteRoomRequest { Room = name });
		}
	}
}
