namespace Conduit.Recording.Services;

public class RecordingFiles(IConfiguration config)
{
	private readonly string _directory = config["Recording:Directory"] ?? "recordings";

	public string GetPath(string fileKey)
	{
		return Path.Combine(_directory, Path.GetFileName(fileKey));
	}

	public bool Exists(string fileKey)
	{
		return File.Exists(GetPath(fileKey));
	}

	public void Delete(string fileKey)
	{
		File.Delete(GetPath(fileKey));
	}
}
