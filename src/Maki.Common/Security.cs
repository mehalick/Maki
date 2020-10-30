using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;

namespace Maki.Common
{
	public static class Security
	{
		public static string GetHash(params string[] items)
		{
			return Convert.ToBase64String(KeyDerivation.Pbkdf2(
				password: string.Join("|", items),
				salt: Encoding.UTF8.GetBytes("YmxbFlvfSrroLLmuBZQBsSpfALOKVmRvKsdneR9dFpOGOFsafAJHMXzvtMGP"),
				prf: KeyDerivationPrf.HMACSHA512,
				iterationCount: 100_000,
				numBytesRequested: 256 / 8));
		}
	}
}
