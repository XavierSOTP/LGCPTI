using Microsoft.Extensions.Logging;
using NLog;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Logger provider implementation that integrates NLog with the Microsoft.Extensions.Logging framework.
/// Adapts NLog loggers for use within dependency injection containers.
/// </summary>
public sealed class PluginLoggerProvider : IPluginLoggerProvider
{
    private readonly LogFactory _logFactory;

    public PluginLoggerProvider(
        LogFactory logFactory)
    {
        _logFactory = logFactory;
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(
        string categoryName)
    {
        var logger =
            _logFactory.GetLogger(
                categoryName);

        return new NLogPluginLogger(
            logger);
    }

    public void Dispose()
    {
    }

    private sealed class NLogPluginLogger :
        Microsoft.Extensions.Logging.ILogger
    {
        private readonly NLog.Logger _logger;

        public NLogPluginLogger(
            NLog.Logger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(
            TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(
            Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel !=
                Microsoft.Extensions.Logging.LogLevel.None;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) ||
                formatter == null)
            {
                return;
            }

            var message =
                formatter(
                    state,
                    exception);

            var nlogLevel =
                MapLogLevel(
                    logLevel);

            _logger.Log(
                nlogLevel,
                exception,
                message);
        }

        private static NLog.LogLevel MapLogLevel(
            Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace =>
                    NLog.LogLevel.Trace,

                Microsoft.Extensions.Logging.LogLevel.Debug =>
                    NLog.LogLevel.Debug,

                Microsoft.Extensions.Logging.LogLevel.Information =>
                    NLog.LogLevel.Info,

                Microsoft.Extensions.Logging.LogLevel.Warning =>
                    NLog.LogLevel.Warn,

                Microsoft.Extensions.Logging.LogLevel.Error =>
                    NLog.LogLevel.Error,

                Microsoft.Extensions.Logging.LogLevel.Critical =>
                    NLog.LogLevel.Fatal,

                _ =>
                    NLog.LogLevel.Off
            };
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } =
            new();

        public void Dispose()
        {
        }
    }
}
