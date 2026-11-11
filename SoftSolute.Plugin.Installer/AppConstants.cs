namespace SoftSolute.Plugin.Installer;
internal static class AppConstants
{
    public const string TARGET_VERSION = "5.13";
    public const string NET_COMMAND = "net";
    public const string SHELL_COMMAND = "cmd";
    public const string SECURITY_DESK_PROCESS_NAME = "SecurityDesk.exe";
    public const string CONFIG_TOOL_PROCESS_NAME = "ConfigTool.exe";
    public const string SCPATH = "SSCPath";
    public const string NO_ARGUMENTS = "No command specified. Use 'install' or 'uninstall'.";
    public const string NO_SCPATH_PATH = "The Security Center Path environment variable SCPath is not defined.";
    public const string SECURITY_CENTER_VERSION_NOT_FOUND = "Security Center version {0} not found in SCPath: {1}";
    public const string UNKOWN_COMMAND = "Unknown command: {0}";
    public const string STOPPING_GENETECT_SERVER = "Stopping GenetecServer...";
    public const string STOP_GENETEC_SERVER_ACTION = "stop GenetecServer";
    public const string START_GENETEC_SERVER_ACTION = "start GenetecServer";
    public const string FOUND_CLIENT_DLL = "Client DLL: {ClientDll}";
    public const string FOUND_SERVER_DLL = "Server DLL: {ServerDll}";
    public const string NOT_FOUND = "(Not found)";
    public const string COULD_NOT_FIND_CLIENT_DLL = "Could not find client module DLL.";
    public const string COULD_NOT_FIND_SERVER_DLL = "Could not find server module DLL.";
    public const string CLIENT_FULL_PATH_NOT_FOUND = "Could not find full client path.";
    public const string SERVER_FULL_PATH_NOT_FOUND = "Could not find full server path.";

    public static class Errors
    {
        public const string ERROR_DURING_INSTALLATION = "An error occurred during the installation process.";
    }

    public static class ExitCodes
    {
        public const int SUCCESS = 0;
        public const int USER_CANCELED = 1602;
        public const int FATAL_ERROR = 1603;
        public const int INVALID_COMMAND_LINE = 1639;
        public const int RESTART_REQUIRED = 3010;
    }

    public static class Installer
    {
        public const string INSTALL = "install";
        public const string UNINSTALL = "uninstall";
        public const string STARTING_INSTALL = "Starting install process...";
        public const string STARTING_UNINSTALL = "Starting uninstall process...";
        public const string STARTED = "Installer started at {0}";
        public const string COMPLETED = "Installer completed successfully at {0}";
        public const string ERROR = "Installer failed: {0}";
        public const string SETTING_REGISTRY_KEYS = "Setting registry keys...";
        public const string RESTART_GENETECT_SERVER = "Restarting GenetecServer...";
        public const string INITIALIZE_PLUGIN_PATHS = "Initialize Plugin Paths";
        public const string CLIENT_PATH = "Client path: {0}";
        public const string SERVER_PATH = "Server path: {0}";
        public const string SETTING_REGISTRY = "Setting registry keys for project: {ProjectName}";
        public const string ENABLING_PLUGING_REGISTRY = "Enabling plugin (64-bit and 32-bit views)...";
        public const string PREPARING_RUN_COMMAND = "Preparing to run command: {NetCommand} {Arguments}";
        public const string EXECUTING_COMMAND = "Executing process: {NetCommand} {Arguments}";
        public const string COMMAND_EXITED = "Process {NetCommand} exited with code: {ExitCode}";
        public const string PROCESS_COMMAND_DID_NOT_START = "Process for {NetCommand} did not start correctly.";
        public const string ERROR_RUNNING_COMMAND = "Error running command: {NetCommand} {Arguments}";

        public const string RESOLVED_ROOT_PATH = "Resolved root path to delete: {RootPath}";
        public const string DELETING_FOLDER = "Deleting folder: {RootPath}";
        public const string FOLDER_NOT_FOUND_TO_DELETE = "Folder not found, nothing to delete.";
        public const string FAILED_TO_UNINSTALL = "Failed to uninstall";
        public const string FAILED_TO_CLEAN_REGISTRY = "Failed to Clean Genetec Plugin Registry";


    }

    public static class Installation
    {
        public const string KILLING_SECURITYDESK_EXE_AND_CONFIGTOOL_EXE = "Killing SecurityDesk.exe and ConfigTool.exe processes...";
        public const string LAUNCHING_SECURITYDESK_EXE_AND_CONFIGTOOL_EXE = "Launching SecurityDesk and ConfigTool...";

    }

    public static class Uninstallation
    {

    }
}
