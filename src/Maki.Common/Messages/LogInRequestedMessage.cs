using System.Text;
using System.Text.Json;

namespace Maki.Common.Messages
{
	public class LogInRequestedMessage
	{
		public string AuthUrl { get; set; } = null!;
		public string Email { get; set; } = null!;

		public byte[] ToBytes()
		{
			var json = JsonSerializer.Serialize(this);

			return Encoding.UTF8.GetBytes(json);
		}
	}
}
