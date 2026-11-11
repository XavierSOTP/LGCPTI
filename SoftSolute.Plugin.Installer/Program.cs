using Microsoft.Win32;
using NLog;
using SoftSolute.Logging;
using SoftSolute.Plugin.Installer.InstallerForms;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SoftSolute.Plugin.Installer;

/// <summary>
/// Entry point for the application.
/// </summary>
internal static class Program
{
    const string PLUGIN_PROJECT_NAME = "LCIPluginExeV1";
    const string PLUGIN_CLIENT_PROJECT_NAME = @"LGC.PTI.Plugin.Client";
    const string PLUGIN_SERVER_PROJECT_NAME = @"LGC.PTI.App.Plugin.Server";
    const string PLUGIN_CLIENT_DLL_PATTERN = "*Plugin.Client.dll";
    const string PLUGIN_SERVER_DLL_PATTERN = "*Plugin.Server.dll";

    private static ILogger _logger;

    /// <summary>
    /// Configures the NLog logging system for the application.
    /// </summary>
    /// <param name="path">The base directory path for log files.</param>
    /// <returns>A configured Logger instance.</returns>
    private static Logger ConfigureLogging(string path)
    {
        LogManager.Configuration = DefaultNLogConfigurator.New()
            .CreateConfig(NLog.LogLevel.Debug, config =>
            {
                LogManagerWrapper.CreateDailyAndArchiveLogFile(
                    config,
                    baseDirectory: path
                );
            });

        return LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Main entry point for the application. Handles installation and uninstallation commands.
    /// </summary>
    /// <param name="args">Command-line arguments. First argument should be "install" or "uninstall".</param>
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var scPath = Environment.GetEnvironmentVariable(AppConstants.SCPATH);

            _logger = ConfigureLogging(basePath);
            _logger.Info($"{AppConstants.Installer.STARTED} - {basePath}");

            if (args.Length == 0)
            {
                _logger.Warn(AppConstants.NO_ARGUMENTS);
                return AppConstants.ExitCodes.INVALID_COMMAND_LINE;
            }

            var command = args[0].ToLowerInvariant();
            _logger.Info($"Command is {command}");

            if (string.IsNullOrWhiteSpace(scPath))
            {
                _logger.Error(AppConstants.NO_SCPATH_PATH);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var form = new PathInputForm("Please select the Security Center installation path:"))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        scPath = form.SelectedPath;
                        Environment.SetEnvironmentVariable(AppConstants.SCPATH, scPath, EnvironmentVariableTarget.Machine);
                    }
                    else
                    {
                        _logger.Error("User cancelled path input.");
                        return AppConstants.ExitCodes.USER_CANCELED;
                    }
                }
            }

            if (!scPath.Contains(AppConstants.TARGET_VERSION))
            {
                string message = string.Format(AppConstants.SECURITY_CENTER_VERSION_NOT_FOUND, AppConstants.TARGET_VERSION, scPath);
                _logger.Error(message);
                return AppConstants.ExitCodes.FATAL_ERROR;
            }

            _logger.Info("Environment user: ", Environment.UserName);

            switch (command)
            {
                case AppConstants.Installer.INSTALL:
                    _logger.Info(AppConstants.Installer.STARTING_INSTALL);
                    Install(basePath, scPath);
                    break;

                case AppConstants.Installer.UNINSTALL:
                    string baseTemp = Path.GetTempPath();
                    string folder = Path.Combine(baseTemp, PLUGIN_PROJECT_NAME);
                    _logger = ConfigureLogging(folder);
                    _logger.Info(AppConstants.Installer.STARTING_UNINSTALL);
                    if (!Uninstall(basePath, scPath))
                        return AppConstants.ExitCodes.FATAL_ERROR;
                    break;

                default:
                    _logger.Error(AppConstants.UNKOWN_COMMAND, command);
                    return AppConstants.ExitCodes.INVALID_COMMAND_LINE;
            }
            _logger.Info(AppConstants.Installer.COMPLETED, scPath);
            return AppConstants.ExitCodes.SUCCESS;
        }
        catch (FileNotFoundException ex)
        {
            _logger.Error(ex, AppConstants.Errors.ERROR_DURING_INSTALLATION);
            return AppConstants.ExitCodes.FATAL_ERROR;
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error(ex, AppConstants.Errors.ERROR_DURING_INSTALLATION);
            return AppConstants.ExitCodes.FATAL_ERROR;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppConstants.Errors.ERROR_DURING_INSTALLATION);
            return AppConstants.ExitCodes.FATAL_ERROR;
        }
    }

    /// <summary>
    /// Performs the installation process for the plugin.
    /// </summary>
    /// <param name="baseDir">The base directory of the installer application.</param>
    /// <param name="scPath">The Security Center installation path.</param>
    private static void Install(string baseDir, string scPath)
    {
        _logger.Info(AppConstants.Installer.INITIALIZE_PLUGIN_PATHS);
        var (registryClientPath, registryServerPath) = InitializePluginPaths(baseDir);

        ShutdownGenetecApplications();

        _logger.Info(AppConstants.Installer.SETTING_REGISTRY_KEYS);
        SetRegistry(PLUGIN_PROJECT_NAME, registryClientPath, registryServerPath);

        StartGenetecApplications(scPath);

        _logger.Info(AppConstants.Installer.COMPLETED, scPath);
    }

    /// <summary>
    /// Starts the Genetec Security Center applications after installation or uninstallation.
    /// </summary>
    /// <param name="scPath">The Security Center installation path.</param>
    private static void StartGenetecApplications(string scPath)
    {
        var securityDeskPath = Path.Combine(scPath, AppConstants.SECURITY_DESK_PROCESS_NAME);
        var configToolPath = Path.Combine(scPath, AppConstants.CONFIG_TOOL_PROCESS_NAME);

        _logger.Info(AppConstants.Installer.RESTART_GENETECT_SERVER);
        RunCommand(AppConstants.NET_COMMAND, AppConstants.START_GENETEC_SERVER_ACTION);

        _logger.Info(AppConstants.Installation.LAUNCHING_SECURITYDESK_EXE_AND_CONFIGTOOL_EXE);
        RunCommand(AppConstants.SHELL_COMMAND, $"/c start \"\" \"{securityDeskPath}\" /u admin /p a /d localhost /nosplash");
        RunCommand(AppConstants.SHELL_COMMAND, $"/c start \"\" \"{configToolPath}\" /u admin /p a /d localhost /nosplash");
    }

    /// <summary>
    /// Shuts down all running Genetec Security Center applications.
    /// </summary>
    private static void ShutdownGenetecApplications()
    {
        _logger.Info(AppConstants.Installation.KILLING_SECURITYDESK_EXE_AND_CONFIGTOOL_EXE);
        KillProcess(AppConstants.SECURITY_DESK_PROCESS_NAME);
        KillProcess(AppConstants.CONFIG_TOOL_PROCESS_NAME);

        _logger.Info(AppConstants.STOPPING_GENETECT_SERVER);
        RunCommand(AppConstants.NET_COMMAND, AppConstants.STOP_GENETEC_SERVER_ACTION);
    }

    /// <summary>
    /// Initializes and validates the paths to the client and server plugin DLLs.
    /// </summary>
    /// <param name="baseDir">The base directory of the installer application.</param>
    /// <returns>A tuple containing the full paths to the client and server plugin DLLs.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the client or server DLL cannot be found.</exception>
    private static (string registryClientPath, string registryServerPath) InitializePluginPaths(string baseDir)
    {
        string clientPath = Path.Combine(baseDir, "..", PLUGIN_CLIENT_PROJECT_NAME);
        string serverPath = Path.Combine(baseDir, "..", PLUGIN_SERVER_PROJECT_NAME);

        _logger.Info(AppConstants.Installer.CLIENT_PATH, clientPath);
        _logger.Info(AppConstants.Installer.SERVER_PATH, serverPath);

        string clientDll = FindDll(clientPath, PLUGIN_CLIENT_DLL_PATTERN);
        string serverDll = FindDll(serverPath, PLUGIN_SERVER_DLL_PATTERN);

        _logger.Info(AppConstants.FOUND_CLIENT_DLL, clientDll ?? AppConstants.NOT_FOUND);
        _logger.Info(AppConstants.FOUND_SERVER_DLL, serverDll ?? AppConstants.NOT_FOUND);

        if (string.IsNullOrEmpty(clientDll))
        {
            _logger.Error(AppConstants.COULD_NOT_FIND_CLIENT_DLL);
            throw new FileNotFoundException(clientDll);
        }

        if (string.IsNullOrEmpty(serverDll))
        {
            _logger.Error(AppConstants.COULD_NOT_FIND_SERVER_DLL);
            throw new FileNotFoundException(serverDll);
        }

        var registryClientPath = Path.Combine(clientPath, clientDll);
        if (string.IsNullOrEmpty(registryClientPath))
        {
            _logger.Error(AppConstants.CLIENT_FULL_PATH_NOT_FOUND);
            throw new FileNotFoundException(registryClientPath);
        }

        var registryServerPath = Path.Combine(serverPath, serverDll);

        if (string.IsNullOrEmpty(registryServerPath))
        {
            _logger.Error(AppConstants.SERVER_FULL_PATH_NOT_FOUND);
            throw new FileNotFoundException(registryServerPath);

        }

        return (registryClientPath, registryServerPath);
    }

    /// <summary>
    /// Forcefully terminates a process by name.
    /// </summary>
    /// <param name="processName">The name of the process to kill.</param>
    private static void KillProcess(string processName)
    {
        RunCommand("taskkill", $"/IM {processName} /F");
    }

    /// <summary>
    /// Finds a DLL matching the specified pattern in the given directory.
    /// </summary>
    /// <param name="path">The directory path to search in.</param>
    /// <param name="pattern">The search pattern for the DLL file.</param>
    /// <returns>The file name of the first matching DLL, or null if none is found.</returns>
    private static string FindDll(string path, string pattern)
    {
        var files = Directory.GetFiles(path, pattern);
        return files.Length > 0 ? Path.GetFileName(files[0]) : null;
    }

    /// <summary>
    /// Sets registry keys required for the plugin installation.
    /// </summary>
    /// <param name="projectName">The plugin project name.</param>
    /// <param name="registryClientPath">The full path to the client plugin DLL.</param>
    /// <param name="registryServerPath">The full path to the server plugin DLL.</param>
    private static void SetRegistry(string projectName, string registryClientPath, string registryServerPath)
    {
        _logger.Info(AppConstants.Installer.SETTING_REGISTRY, projectName);

        RegisterClient(projectName, registryClientPath);
        RegisterServer(projectName, registryServerPath);

        EnablePluginInRegistry(projectName);
    }

    /// <summary>
    /// Registers the client module DLL in the Windows registry.
    /// </summary>
    /// <param name="projectName">The plugin project name.</param>
    /// <param name="registryClientPath">The full path to the client plugin DLL.</param>
    private static void RegisterClient(string projectName, string registryClientPath)
    {
        var correctPath = Path.GetFullPath(registryClientPath);
        string regBase = $@"HKLM\SOFTWARE\Genetec\Security Center\Plugins\{projectName}";

        _logger.Info("Setting client module registry (64-bit view): {DllPath}", correctPath);
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""ClientModule"" /t REG_SZ /d ""{correctPath}"" /reg:64");

        _logger.Info("Setting client module registry (32-bit view): {DllPath}", correctPath);
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""ClientModule"" /t REG_SZ /d ""{correctPath}"" /reg:32");
    }

    /// <summary>
    /// Registers the server module DLL in the Windows registry.
    /// </summary>
    /// <param name="projectName">The plugin project name.</param>
    /// <param name="registryServerPath">The full path to the server plugin DLL.</param>
    private static void RegisterServer(string projectName, string registryServerPath)
    {
        var correctPath = Path.GetFullPath(registryServerPath);
        string regBase = $@"HKLM\SOFTWARE\Genetec\Security Center\Plugins\{projectName}";

        _logger.Info("Setting server module registry (64-bit view): {DllPath}", correctPath);
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""ServerModule"" /t REG_SZ /d ""{correctPath}"" /reg:64");

        _logger.Info("Setting server module registry (32-bit view): {DllPath}", correctPath);
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""ServerModule"" /t REG_SZ /d ""{correctPath}"" /reg:32");
    }

    /// <summary>
    /// Enables the plugin in the Windows registry by setting the Enabled flag to True.
    /// </summary>
    /// <param name="projectName">The plugin project name.</param>
    private static void EnablePluginInRegistry(string projectName)
    {
        _logger.Info(AppConstants.Installer.ENABLING_PLUGING_REGISTRY);
        string regBase = $@"HKLM\SOFTWARE\Genetec\Security Center\Plugins\{projectName}";
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""Enabled"" /t REG_SZ /d ""True"" /reg:64");
        RunCommand("reg", $@"ADD ""{regBase}"" /f /v ""Enabled"" /t REG_SZ /d ""True"" /reg:32");
    }

    /// <summary>
    /// Runs an external command with the specified arguments.
    /// </summary>
    /// <param name="fileName">The name or path of the command to execute.</param>
    /// <param name="arguments">Command-line arguments to pass to the command.</param>
    private static void RunCommand(string fileName, string arguments)
    {
        _logger.Info(AppConstants.Installer.PREPARING_RUN_COMMAND, fileName, arguments);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };

            _logger.Info(AppConstants.Installer.EXECUTING_COMMAND, fileName, arguments);
            using var proc = Process.Start(psi);
            proc?.WaitForExit();

            if (proc != null)
            {
                _logger.Info(AppConstants.Installer.COMMAND_EXITED, fileName, proc.ExitCode);
            }
            else
            {
                _logger.Warn(AppConstants.Installer.PROCESS_COMMAND_DID_NOT_START, fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppConstants.Installer.ERROR_RUNNING_COMMAND, fileName, arguments);
        }
    }

    /// <summary>
    /// Performs the uninstallation process for the plugin.
    /// </summary>
    /// <param name="baseDir">The base directory of the installer application.</param>
    /// <param name="scPath">The Security Center installation path.</param>
    private static bool Uninstall(string baseDir, string scPath)
    {
        try
        {
            _logger.Info(AppConstants.Installer.UNINSTALL);

            string rootPath = Path.GetFullPath(Path.Combine(baseDir, ".."));
            _logger.Info(AppConstants.Installer.RESOLVED_ROOT_PATH, rootPath);

            ShutdownGenetecApplications();
            CleanGenetecPlugin();
            StartGenetecApplications(scPath);

            if (Directory.Exists(rootPath))
            {
                _logger.Warn(AppConstants.Installer.DELETING_FOLDER, rootPath);
                Directory.Delete(rootPath, true);
                _logger.Info(AppConstants.Installer.DELETING_FOLDER);
            }
            else
            {
                _logger.Info(AppConstants.Installer.FOLDER_NOT_FOUND_TO_DELETE);
            }

            /// <summary>
            /// Removes the plugin registry entries from the Windows registry.
            /// </summary>
            static void CleanGenetecPlugin()
            {
                string[] basePaths = { @"SOFTWARE\Genetec\Security Center\Plugins", @"SOFTWARE\Wow6432Node\Genetec\Security Center\Plugins" };

                foreach (var basePath in basePaths)
                {
                    using (var baseKey = Registry.LocalMachine.OpenSubKey(basePath, writable: true))
                    {
                        if (baseKey == null)
                        {
                            continue;
                        }

                        try
                        {
                            baseKey.DeleteSubKeyTree(PLUGIN_PROJECT_NAME, throwOnMissingSubKey: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, AppConstants.Installer.FAILED_TO_CLEAN_REGISTRY);
                        }
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppConstants.Installer.FAILED_TO_UNINSTALL);
            return false;
        }
    }
}
