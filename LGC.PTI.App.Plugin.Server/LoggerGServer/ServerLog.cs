using NLog;
using System.Runtime.CompilerServices;
using Logger = NLog.Logger;

/// <summary>
/// Provides static logging methods for the Plugin Server using NLog.
/// Automatically includes the calling class name in log messages.
/// </summary>
public static class ServerLog
{
    private static readonly Logger _log = LogManager.GetLogger("SERVER");

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Info(string msg, [CallerFilePath] string filePath = "")
        => _log.Info(FormatMessage(msg, filePath));

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Warn(string msg, [CallerFilePath] string filePath = "")
        => _log.Warn(FormatMessage(msg, filePath));

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Error(string msg, [CallerFilePath] string filePath = "")
        => _log.Error(FormatMessage(msg, filePath));

    /// <summary>
    /// Logs an error message with an associated exception.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Error(Exception ex, string msg, [CallerFilePath] string filePath = "")
        => _log.Error(ex, FormatMessage(msg, filePath));

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Debug(string msg, [CallerFilePath] string filePath = "")
        => _log.Debug(FormatMessage(msg, filePath));

    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="filePath">The source file path (automatically populated by the compiler).</param>
    public static void Trace(string msg, [CallerFilePath] string filePath = "")
        => _log.Trace(FormatMessage(msg, filePath));

    /// <summary>
    /// Creates a scoped logger with a specific context name (e.g., "SERVER.PluginModule").
    /// </summary>
    /// <param name="scope">The scope name to append to the "SERVER" logger prefix.</param>
    /// <returns>A new <see cref="Logger"/> instance with the scoped name.</returns>
    public static Logger CreateScopedLogger(string scope) => LogManager.GetLogger($"SERVER.{scope}");

    /// <summary>
    /// Formats the log message by prepending the calling class name.
    /// </summary>
    /// <param name="msg">The original message.</param>
    /// <param name="filePath">The source file path to extract the class name from.</param>
    /// <returns>A formatted message with the class name prefix.</returns>
    private static string FormatMessage(string msg, string filePath)
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        return $"[{className}] {msg}";
    }
}

