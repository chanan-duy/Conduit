using Conduit.Recording.Data;
using Livekit.Server.Sdk.Dotnet;

namespace Conduit.Recording.Services;

public class EgressClient
{
	private readonly EgressServiceClient _egress;
	private readonly string _botPageUrl;
	private readonly string _outputDirectory;

	public EgressClient(IConfiguration config)
	{
		var apiUrl = config["LiveKit:ApiUrl"] ?? throw new InvalidOperationException("'LiveKit:ApiUrl' is not configured.");
		var apiKey = config["LiveKit:ApiKey"] ?? throw new InvalidOperationException("'LiveKit:ApiKey' is not configured.");
		var apiSecret = config["LiveKit:ApiSecret"] ?? throw new InvalidOperationException("'LiveKit:ApiSecret' is not configured.");
		_botPageUrl = config["Recording:BotPageUrl"] ?? throw new InvalidOperationException("'Recording:BotPageUrl' is not configured.");
		_outputDirectory = config["Recording:EgressOutputDirectory"] ?? "/out";

		_egress = new EgressServiceClient(apiUrl, apiKey, apiSecret);
	}

	public async Task<string> StartAsync(Guid meetingId, string exchangeToken, string fileKey)
	{
		var info = await _egress.StartRoomCompositeEgress(new RoomCompositeEgressRequest
		{
			RoomName = meetingId.ToString(),
			CustomBaseUrl = $"{_botPageUrl.TrimEnd('/')}/{meetingId}?exchangeToken={Uri.EscapeDataString(exchangeToken)}",
			Preset = EncodingOptionsPreset.H264720P30,
			FileOutputs =
			{
				new EncodedFileOutput
				{
					FileType = EncodedFileType.Mp4,
					Filepath = $"{_outputDirectory}/{fileKey}",
				},
			},
		});

		return info.EgressId;
	}

	public async Task<EgressInfo?> FindAsync(string egressId)
	{
		var list = await _egress.ListEgress(new ListEgressRequest { EgressId = egressId });
		return list.Items.FirstOrDefault();
	}

	public async Task StopAsync(string egressId)
	{
		await _egress.StopEgress(new StopEgressRequest { EgressId = egressId });
	}

	public static SessionStatus ToSessionStatus(EgressStatus status)
	{
		return status switch
		{
			EgressStatus.EgressStarting => SessionStatus.Starting,
			EgressStatus.EgressActive or EgressStatus.EgressEnding => SessionStatus.Recording,
			EgressStatus.EgressComplete or EgressStatus.EgressLimitReached => SessionStatus.Completed,
			_ => SessionStatus.Failed,
		};
	}
}
