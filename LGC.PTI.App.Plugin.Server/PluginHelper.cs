namespace LGC.PTI.App.Plugin.Server;

public partial class PluginModule
{
    public static class PluginHelper
    {
        public static string ValidateAndCreateExternalAppPath(NotEmptyString PluginPath)
        {
            if (string.IsNullOrWhiteSpace(PluginPath))
                throw new InvalidOperationException("PluginPath is not set in the plugin configuration.");

            // Append subfolder and exe file
            const string relativePath = "SnowApp/SoftSolute.Snow.Genetec.App.exe";
            var fullPath = Path.Combine(PluginPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException($"The external application was not found at: {fullPath}");

            return fullPath;
        }

        public static string GetOrCreateQueueFolderFromPluginPath(NotEmptyString appExecPath)
        {
            string transactAppFolder = Path.GetDirectoryName(appExecPath);

            string queueFolder = Path.Combine(transactAppFolder, "Queue");

            if (!Directory.Exists(queueFolder))
                Directory.CreateDirectory(queueFolder);

            return queueFolder;
        }

        public static string CreateErrorQueuePath(string pluginPath)
        {
            if (string.IsNullOrWhiteSpace(pluginPath))
                throw new InvalidOperationException("PluginPath is not set in the plugin configuration.");

            const string relativePath = "ErrorQueue";
            var fullPath = Path.Combine(pluginPath, relativePath);

            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        public static string ValidateAndCreateAppSettingsPath(string PluginPath)
        {
            if (string.IsNullOrWhiteSpace(PluginPath))
                throw new InvalidOperationException("PluginPath is not set in the plugin configuration.");

            // Append subfolder and exe file
            const string relativePath = "appsettings.ServerPlugin.json";
            var fullPath = Path.Combine(PluginPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException($"The appSettings was not found at: {fullPath}");

            return fullPath;
        }
    }
}