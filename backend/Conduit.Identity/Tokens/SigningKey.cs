using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Identity.Tokens;

public sealed class SigningKey
{
	public RsaSecurityKey Key { get; }
	public SigningCredentials Credentials { get; }
	public object PublicJwk { get; }

	private SigningKey(RSA rsa)
	{
		var publicParameters = rsa.ExportParameters(false);
		var keyId = Base64UrlEncoder.Encode(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()))[..16];

		Key = new RsaSecurityKey(rsa) { KeyId = keyId };
		Credentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256);
		PublicJwk = new
		{
			kty = "RSA",
			use = "sig",
			alg = SecurityAlgorithms.RsaSha256,
			kid = keyId,
			n = Base64UrlEncoder.Encode(publicParameters.Modulus),
			e = Base64UrlEncoder.Encode(publicParameters.Exponent),
		};
	}

	public static SigningKey LoadOrCreate(string path)
	{
		var rsa = RSA.Create();
		if (File.Exists(path))
		{
			rsa.ImportFromPem(File.ReadAllText(path));
		}
		else
		{
			rsa.KeySize = 2048;
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
			File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
		}

		return new SigningKey(rsa);
	}
}
