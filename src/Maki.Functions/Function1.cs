using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Maki.Functions
{
	public class Function1 : FunctionBase<Function1>
	{
		private readonly IConfiguration _configuration;

		public Function1(ILogger logger, IConfiguration configuration) : base(logger)
		{
			_configuration = configuration;
		}

		[FunctionName(nameof(Function1))]
		public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest _)
		{
			using (Init())
			{
				var responseMessage = _configuration["Maki:Connections:ServiceBus"];

				return new OkObjectResult(responseMessage);
			}
		}
	}
}
