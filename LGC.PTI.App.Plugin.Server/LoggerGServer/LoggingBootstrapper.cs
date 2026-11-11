using NLog;
using NLog.Config;
using NLog.Targets;

namespace LGC.PTI.App.Plugin.Server.LoggerGServer;

/// <summary>
/// Provides initialization and configuration for the NLog logging system used by the Plugin Server.
/// </summary>
public static class LoggingBootstrapper
{
    private static bool _initialized;
    private static string _logDirectory;

    /// <summary>
    /// Gets the directory path where log files are stored.
    /// </summary>
    public static string LogDirectory => _logDirectory;

    /// <summary>
    /// Initializes the logging configuration for the Plugin Server.
    /// Creates the log directory if it doesn't exist and configures NLog with file targets.
    /// </summary>
    /// <param name="customLogPath">
    /// Optional custom path for log files. If <c>null</c>, defaults to a "Logs\PluginServer" 
    /// subdirectory relative to the executing assembly's location.
    /// </param>
    /// <remarks>
    /// <para>This method is idempotent and will only initialize logging once.</para>
    /// <para>Log files are configured with the following settings:</para>
    /// <list type="bullet">
    ///     <item><description>Daily file rotation with format: pluginServer.yyyyMMdd.log</description></item>
    ///     <item><description>Maximum of 7 archived log files</description></item>
    ///     <item><description>Logs are captured for loggers named "SERVER" and "SERVER.*"</description></item>
    /// </list>
    /// </remarks>
    public static void Init(string customLogPath = null)
    {
        if (_initialized) return;

        _logDirectory = customLogPath ?? Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "Logs",
            "PluginServer"
        );

        Directory.CreateDirectory(_logDirectory);

        var config = LogManager.Configuration ?? new LoggingConfiguration();

        var serverFileTarget = new FileTarget("pluginServerFile")
        {
            FileName = Path.Combine(_logDirectory, "pluginServer.${date:format=yyyyMMdd}.log"),
            ArchiveFileName = Path.Combine(_logDirectory, "archive", "pluginServer.{#}.log"),
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 7,
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}",
            KeepFileOpen = false
        };

        config.AddTarget("pluginServerFile", serverFileTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, serverFileTarget, "SERVER");
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, serverFileTarget, "SERVER.*");

        LogManager.Configuration = config;
        LogManager.ReconfigExistingLoggers();

        ServerLog.Info($"Plugin Server Logger initialized at: {_logDirectory}");

        _initialized = true;
    }
}
