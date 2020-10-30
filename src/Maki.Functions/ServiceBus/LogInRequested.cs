using Maki.Common;
using Maki.Common.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using SendGrid.Helpers.Mail;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Maki.Functions.ServiceBus
{
	[ServiceBusAccount("Maki:Connections:ServiceBus")]
	public class LogInRequested : FunctionBase<LogInRequested>
	{
		private readonly IConfiguration _configuration;

		public LogInRequested(ILogger logger, IConfiguration configuration) : base(logger)
		{
			_configuration = configuration;
		}

		[FunctionName(nameof(LogInRequested))]
		public async Task Run(
			[ServiceBusTrigger("login-requested")] string json,
			[SendGrid(ApiKey = "Maki:SendGrid:ApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
			CancellationToken cancellationToken)
		{
			using (Init<LogInRequestedMessage>(json, out var message))
			{
				var uriBuilder = new UriBuilder(message.AuthUrl);

				var emailHash = Security.GetHash(message.Email);
				var timestamp = DateTime.UtcNow.Ticks.ToString();
				var queryHash = Security.GetHash(emailHash, timestamp);

				var query = HttpUtility.ParseQueryString(uriBuilder.Query);
				query["id"] = emailHash;
				query["timestamp"] = timestamp;
				query["hash"] = queryHash;

				uriBuilder.Query = query.ToString();

				var link = uriBuilder.ToString();

				var mail = new SendGridMessage();
				mail.AddTo(message.Email);
				mail.SetSubject("Maki Log In");
				mail.SetFrom(new EmailAddress("noreply@makilog.com", "Maki"));
				mail.HtmlContent = $"<p>Hello from Maki, to log in please click the link below:</p><p><a clicktracking='off' href='{link}'>{link}</a></p>";

				await messageCollector.AddAsync(mail, cancellationToken);
			}
		}
	}
}
