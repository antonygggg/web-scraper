using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace WebScraper
{
	/// <summary>
	/// Extensions for logging with ILogger
	/// </summary>
	public static class LoggerExtentions
	{
		// default template for log
		private const string _messageTemplate = "[{time}] ({thread}) in {member}: {message}";

		/// <summary>
		/// Log with ILogger
		/// </summary>
		/// <param name="logger">ILogger instance</param>
		/// <param name="message">Log message</param>
		/// <param name="level">Log level</param>
		/// <param name="eventTime">Log time in UTC</param>
		/// <param name="exception">Exception if exists</param>
		/// <param name="memberName">Event from class</param>
		public static void Log(
			this ILogger logger,
			string message,
			LogEventLevel level = LogEventLevel.Information,
			DateTimeOffset? eventTime = null,
			Exception exception = null,
			[CallerMemberName] string memberName = "")
		{
			if (!string.IsNullOrEmpty(message) && exception != null)
			{
				message += $" ({exception.Message})";
			}

			logger.Write(
				level,
				exception,
				_messageTemplate,
				eventTime ?? DateTimeOffset.Now,
				Thread.CurrentThread.ManagedThreadId,
				memberName,
				message);
		}
	}
}
