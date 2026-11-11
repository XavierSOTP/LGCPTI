using NLog.Config;
using NLog.Targets;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Utility helper class for managing plugin directory and logging file operations.
/// </summary>
public static class DirectoryHelper
{
    /// <summary>
    /// Creates and configures daily and archived log files in the NLog configuration.
    /// Sets up automatic daily rotation with archive subdirectories and maximum archive retention.
    /// </summary>
    /// <param name="configuration">The NLog logging configuration to modify.</param>
    /// <param name="baseDirectory">The base directory for log files (default: "${basedir}").</param>
    /// <param name="outputDirectory">The subdirectory for current logs (default: "logs").</param>
    /// <param name="filename">The base filename for log files without extension (default: "debug").</param>
    /// <param name="maxArchiveFiles">The maximum number of archive files to retain (default: 7).</param>
    /// <param name="dateTimeFormat">The date/time format for log file naming (default: "yyyyMMdd").</param>
    public static void CreateDailyAndArchiveLogFile(
      LoggingConfiguration configuration,
      string baseDirectory = "${basedir}",
      string outputDirectory = "logs",
      string filename = "debug",
      int maxArchiveFiles = 7,
      string dateTimeFormat = "yyyyMMdd")
    {
        var fileTarget =
            configuration.FindTargetByName<FileTarget>(
                "logfile");

        fileTarget.FileName =
            $"{baseDirectory}/{outputDirectory}/{filename}.${{date:format={dateTimeFormat}}}.log";

        fileTarget.ArchiveFileName =
            $"{baseDirectory}/{outputDirectory}/archive/{filename}.${{date:format={dateTimeFormat}}}.log";

        fileTarget.ArchiveEvery =
            FileArchivePeriod.Day;

        fileTarget.MaxArchiveFiles =
            maxArchiveFiles;
    }

    /// <summary>
    /// Ensures that a directory exists, creating it if necessary.
    /// Combines the base path and folder name, then creates the directory if it does not exist.
    /// </summary>
    /// <param name="basePath">The base directory path. Cannot be null or empty.</param>
    /// <param name="folderName">The name of the folder to create or verify.</param>
    /// <returns>The full path of the directory.</returns>
    /// <exception cref="ArgumentException">Thrown when basePath is null or empty.</exception>
    public static string EnsureDirectory(string basePath, string folderName)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));

        var fullPath = Path.Combine(basePath, folderName);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        return fullPath;
    }
}