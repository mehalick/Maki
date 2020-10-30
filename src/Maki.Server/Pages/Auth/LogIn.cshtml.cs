using Maki.Common;
using Maki.Common.Messages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Maki.Server.Pages.Auth
{
	public class LogInModel : PageModel
	{
		public LogInAuth.ValidationResult ValidationResult { get; set; } = LogInAuth.ValidationResult.None;

		private readonly HttpRequest _httpRequest;

		public LogInModel(IHttpContextAccessor context)
		{
			_httpRequest = context.HttpContext!.Request;
		}

		public async Task<IActionResult> OnGetAsync([FromQuery] LogInAuth logInAuth)
		{
			if (string.IsNullOrWhiteSpace(logInAuth.Id))
			{
				return Page();
			}

			ValidationResult = logInAuth.Validate();

			if (ValidationResult != LogInAuth.ValidationResult.Success)
			{
				return Page();
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, logInAuth.Id)
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true
			};

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties).ConfigureAwait(false);

			return LocalRedirect("~/");
		}

		public async Task<IActionResult> OnPostAsync(string email)
		{
			var uriBuilder = new UriBuilder
			{
				Scheme = _httpRequest.Scheme,
				Host = _httpRequest.Host.Host,
				Path = "auth/login"
			};

			if (_httpRequest.Host.Port.HasValue)
			{
				uriBuilder.Port = _httpRequest.Host.Port.Value;
			}

			var logInRequestedMessage = new LogInRequestedMessage
			{
				AuthUrl = uriBuilder.ToString(),
				Email = email
			};

			return Content(JsonSerializer.Serialize(logInRequestedMessage));

			var message = new Message(logInRequestedMessage.ToBytes());

			//var client = new MessageSender("Endpoint=sb://maki-app.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=VjOz8aZT3Bpn7gJhRPFVGn+saTLfX0A/kS/sSRRKYT0=", "login-requested");

			//var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
			//var sendClient = new QueueClient("sb://maki-app.servicebus.windows.net/", "login-requested", tokenProvider);

			var queueClient = new QueueClient("Endpoint=sb://maki-app.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=VjOz8aZT3Bpn7gJhRPFVGn+saTLfX0A/kS/sSRRKYT0=", "login-requested", retryPolicy: RetryPolicy.Default);
			await queueClient.SendAsync(message);
			await queueClient.CloseAsync();

			return RedirectToPage();
		}
	}

	public class LogInAuth
	{
		public string Id { get; set; } = null!;
		public long Timestamp { get; set; }
		public string Hash { get; set; } = null!;

		public enum ValidationResult
		{
			None,
			MissingParameters,
			InvalidHash,
			ExpiredTimestamp,
			Success
		}

		public ValidationResult Validate()
		{
			if (!ValidateParameters())
			{
				return ValidationResult.MissingParameters;
			}

			if (!ValidateHash())
			{
				return ValidationResult.InvalidHash;
			}

			if (!ValidateTimestamp())
			{
				return ValidationResult.ExpiredTimestamp;
			}

			return ValidationResult.Success;
		}

		private bool ValidateParameters()
		{
			return !string.IsNullOrWhiteSpace(Id) && !Timestamp.Equals(default) && !string.IsNullOrWhiteSpace(Hash);
		}

		private bool ValidateHash()
		{
			return Security.GetHash(Id, Timestamp.ToString()) == Hash;
		}

		private bool ValidateTimestamp()
		{
			try
			{
				var dt = new DateTime(Timestamp);
				var now = DateTime.UtcNow;
				return now.Subtract(dt).TotalMilliseconds <= 15 * 60 * 1000;
			}
			catch
			{
				return false;
			}
		}
	}
}
