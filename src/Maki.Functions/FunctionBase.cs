using Serilog;
using SerilogTimings.Extensions;
using System;
using System.Text.Json;

namespace Maki.Functions
{
	public abstract class FunctionBase<T>
	{
		public FunctionLog Log { get; set; }

		private readonly string _functionName;
		private readonly ILogger _logger;

		protected FunctionBase(ILogger logger)
		{
			_logger = logger;
			_functionName = typeof(T).Name;

			Log = new FunctionLog(logger, _functionName);
		}

		protected IDisposable Init()
		{
			return _logger.TimeOperation("<{FunctionName:l}> Executing function", _functionName);
		}

		protected IDisposable Init<TMessage>(string json, out TMessage message)
		{
			message = JsonSerializer.Deserialize<TMessage>(json)!;

			return _logger.TimeOperation("<{FunctionName:l}> Executing function for {@Message}", _functionName, message);
		}

		public class FunctionLog
		{
			private readonly ILogger _logger;
			private readonly string _functionName;

			public FunctionLog(ILogger logger, string functionName)
			{
				_logger = logger;
				_functionName = functionName;
			}

			public void Information(string messageTemplate, params object[] propertyValues)
			{
				_logger.Information(string.Concat("<{FunctionName:l}> ", messageTemplate), _functionName, propertyValues);
			}
		}
	}
}
