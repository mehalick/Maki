using Azure.Identity;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(Maki.Functions.Startup))]

namespace Maki.Functions
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			var configuration = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.AddAzureAppConfiguration(options =>
				{
					var azConfigUrl = Environment.GetEnvironmentVariable("Maki:Connections:AppConfiguration");

					if (string.IsNullOrWhiteSpace(azConfigUrl))
					{
						return;
					}

					options.Connect(new Uri(azConfigUrl), new ManagedIdentityCredential())
						.Select(KeyFilter.Any, "maki");
				})
				.Build();

			builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configuration));

			var logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Logger", "Serilog")
				.WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
				.CreateLogger();

			builder.Services.AddScoped<ILogger>(_ => logger);
		}
	}
}
