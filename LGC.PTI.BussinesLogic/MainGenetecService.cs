using Genetec.Sdk;
using Genetec.Sdk.Queries;
using global::SoftSolute.Plugin.Common.Configurations;
using global::SoftSolute.Sdk.Common;
using LanguageExt;
using LGC.PIT.Plugin.Common;
using LGC.PIT.Plugin.Common.Configs;
using LGC.PTI.Domain.Common;
using LGC.PTI.Refit.Authentication;
using LGC.PTI.Shared.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftSolute.Common.Utilities.LanguageExtSS;
using SoftSolute.Plugin.Exe.Mappers;
using SoftSolute.Utiities.Plugin.Adapters;
namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Orchestrates the main Genetec SDK service integration for the PTI plugin.
/// Manages service initialization, connection to Security Center, license validation,
/// authentication, and SDK startup pipelines.
/// </summary>
public sealed partial class MainGenetecService(ILogger<MainGenetecService> logger, IServiceProvider container, IUnInitLicenseManager unInitLicenseManager)
{
    #region Properties

    /// <summary>
    /// Gets the plugin execution configuration containing plugin settings and startup data.
    /// </summary>
    private IPluginExeConfig ApplicationSettings { get; } = container.GetRequiredService<IPluginExeConfig>();

    /// <summary>
    /// Gets the SDK settings for Genetec integration.
    /// </summary>
    private ISdkSettings settings { get; } = container.GetRequiredService<ISdkSettings>();

    /// <summary>
    /// Gets the configurable application settings for the PTI plugin.
    /// </summary>
    IConfigurableAppSettings configurableAppSettings { get; } = container.GetRequiredService<IConfigurableAppSettings>();

    /// <summary>
    /// Gets or sets the Genetec SDK engine instance for entity access and operations.
    /// </summary>
    private IEngine Engine { get; set; } = container.GetRequiredService<IEngine>();

    /// <summary>
    /// Gets the logger for diagnostic and informational messages.
    /// </summary>
    private ILogger Logger { get; } = logger;

    /// <summary>
    /// Gets the uninitialized license manager for license validation.
    /// </summary>
    private IUnInitLicenseManager UnInitLicenseManager { get; } =
    unInitLicenseManager;

    /// <summary>
    /// Gets the SDK application instance that manages observers and lifecycle events.
    /// </summary>
    private SdkApplication SdkApplication { get; } = container.GetRequiredService<SdkApplication>();

    /// <summary>
    /// Gets the pipe client for inter-process communication with the plugin server.
    /// </summary>
    private IPipeAppClient PipeAppClient { get; } = container.GetRequiredService<IPipeAppClient>();

    /// <summary>
    /// Gets the configurable application settings for the PTI plugin configuration.
    /// </summary>
    private IConfigurableAppSettings Settings { get; } = container.GetRequiredService<IConfigurableAppSettings>();

    /// <summary>
    /// Gets the authentication credential store managing OAuth credentials and tokens.
    /// </summary>
    private IAuthCredentialStore authCredentialStore { get;  } = container.GetRequiredService<IAuthCredentialStore>();

    /// <summary>
    /// Gets the authentication service for obtaining and managing API access tokens.
    /// </summary>
    private IAuthenticationSrv AuthenticationSrv { get; } =
    container.GetRequiredService<IAuthenticationSrv>();
    #endregion Properties

    #region Public Methods

    /// <summary>
    /// Runs the main service execution pipeline asynchronously.
    /// Performs authentication, license validation, SDK startup, and entity initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous service execution.</returns>
    public async Task RunServiceExecutionAsync()
    {
        Logger.LogInformation(
            "Starting MainGenetecService.");

        Logger.LogInformation(
            "Authentication settings loaded. TokenUrl: {TokenUrl}, ClientId: {ClientId}, HasClientSecret: {HasClientSecret}",
            Settings.LGCSettings.ApiSettings.TokenURL,
            Settings.LGCSettings.ApiSettings.ClientId,
            !string.IsNullOrWhiteSpace(
                Settings.LGCSettings.ApiSettings.ClientSecret));

        authCredentialStore.UpdateCredentials(
            Settings.LGCSettings.ApiSettings.TokenURL,
            Settings.LGCSettings.ApiSettings.ClientId,
            Settings.LGCSettings.ApiSettings.ClientSecret);

        Logger.LogInformation(
            "Requesting initial authentication token.");

        var tokenResult = await AuthenticationSrv
            .TryGetTokenAsync()
            .ConfigureAwait(false);

        if (tokenResult.IsNone)
        {
            Logger.LogError(
                "Initial authentication token could not be obtained.");

            return;
        }

        Logger.LogInformation(
            "Initial authentication token obtained successfully.");

        var startupPipeline =
            from licenseManager in ExecuteLicensePipeline()
            from _3 in ExecuteSdkStartupPipelineAsync()
            select licenseManager;

        var result = await startupPipeline
            .RunAsync()
            .ConfigureAwait(false);

        if (result.IsFail)
        {
            Logger.LogError(
                "Startup pipeline failed.");

            return;
        }

        Logger.LogInformation(
            "MainGenetecService startup completed.");
    }

    #endregion public methods

    /// <summary>
    /// Executes the SDK application startup pipeline including initialization and start handlers.
    /// </summary>
    /// <returns>An asynchronous effect that returns unit on success or propagates errors.</returns>
    private Aff<Unit> ExecuteSdkStartupPipelineAsync()
    {
        var startupPipeline =
            (
                from _1 in SdkApplication.InitializeAsync().Memo()
                from _2 in SdkApplication.HandleStart(CancellationToken.None).Memo()
                select Prelude.unit
            ).Memo();

        startupPipeline.OnSucc(x =>
        {
            _ = Task.Run(() => HandleSuccessAsync());
        });

        return startupPipeline.OnFail(ex =>
        {
            Logger.LogError(
                ex,
                "Error during SDK startup pipeline in {Method}",
                nameof(ExecuteSdkStartupPipelineAsync));
            _ = Task.Run(() => HandlePipelineFailureAsync(ex));
        });
    }

    /// <summary>
    /// Handles successful completion of the service startup pipeline.
    /// Sends success notification via named pipe to the server.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleSuccessAsync()
    {
        Logger.LogInformation($"starting service pipeline on {nameof(RunServiceExecutionAsync)} completed");

        await PipeAppClient.TrySendAsync(
            GetServerPipeName(configurableAppSettings.LocalPluginGuid),
            $"Starting service pipeline",
            PipeMessageType.Info
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles failure of the service startup pipeline.
    /// Logs the error and sends failure notification via named pipe to the server.
    /// </summary>
    /// <param name="ex">The exception that occurred during pipeline execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandlePipelineFailureAsync(Exception ex)
    {
        Logger.LogError(ex, $"Error starting service pipeline on {nameof(RunServiceExecutionAsync)}");
        await PipeAppClient.TrySendAsync(
            GetServerPipeName(configurableAppSettings.LocalPluginGuid),
            $"Error while starting service pipeline. {ex.Message}",
            PipeMessageType.Error
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the connection pipeline to establish a connection to the Genetec Security Center directory.
    /// </summary>
    /// <returns>An asynchronous effect that returns unit on successful connection or forces exit on failure.</returns>
    private Aff<Unit> ExecuteConnectionPipelineAsync()
    {
        var connectionPipeline =
            from _ in ConnectToSCDirectory(container.GetRequiredService<ISdkConnector>())
            select Prelude.unit;

        return connectionPipeline.OnFail(ex =>
        {
            Logger.LogError(
                ex,
                "Error starting connection pipeline in {Method}",
                nameof(ExecuteConnectionPipelineAsync));

            PTIExitManager.Instance.ForceExit(
                Logger,
                "Failed to connect to Security Center Directory");
        });
    }

    /// <summary>
    /// Establishes a connection to the Genetec Security Center directory using the provided connector.
    /// </summary>
    /// <param name="sdkConnector">The SDK connector for Security Center communication.</param>
    /// <returns>An asynchronous effect containing the connected engine instance or failing with connection errors.</returns>
    private Aff<IEngine> ConnectToSCDirectory(ISdkConnector sdkConnector)
    {
        var login = ApplicationSettings.SCLoginData.Map();

        var connection = (from engine in sdkConnector.ConnectToSCDirectoryAsync(login)
                          where engine.IsConnected
                          select engine)
                          .Memo();

        connection
            .OnFail(ex => Logger.LogError(ex, "❌ Error connecting to Security Center with user: {User} at {Directory}", login.User, login.Directory))
            .OnSucc(eng => Engine = eng)
            .RunSynchronous();
        return connection;
    }

    /// <summary>
    /// Executes the license validation pipeline to validate the plugin license.
    /// Retrieves license from embedded resources and validates against the configured license file.
    /// </summary>
    /// <returns>An asynchronous effect containing the initialized license manager on success, or failure with error details.</returns>
    private Aff<ILicenseManagerInitialized> ExecuteLicensePipeline()
    {
        var certificate = LGC.PTI.BussinesLogic.Properties.Resources.SoftSoluteLicenseCrt;
        var licenseFolder = Path.GetDirectoryName(
            typeof(MainGenetecService).Assembly.Location);
        var sdkVersion = new Version(5, 13, 0, 0);
        var pluginName = "QSL PTI";

        var pluginInstanceId = 1000;

        if (pluginInstanceId == 0)
        {
            pluginInstanceId = 1000;
        }

        Logger.LogInformation(
            "Attempting to load license. License folder: {LicenseFolder}, SDK version: {SdkVersion}, Plugin: {PluginName}, Instance ID: {InstanceId}",
            licenseFolder,
            sdkVersion,
            pluginName,
            pluginInstanceId);

        var licensePipeline = UnInitLicenseManager.TryLoadLicense(
            certificate,
            sdkVersion,
            pluginName,
            pluginInstanceId,
            licenseFolder);

        return licensePipeline
            .OnSucc(initializedLicenseManager =>
            {
                Logger.LogInformation(
                    "License validated successfully. Expiration date: {ExpirationDate}",
                    initializedLicenseManager.License.ExpirationDate);
            })
            .OnFail(x =>
            {
                PTITodoCode.Instance.CodeReviewRequired("Add Pipe Client");

                Logger.LogError(
                   x,
                   "License validation failed in {Method}",
                   nameof(ExecuteLicensePipeline));



                _ = Task.Run(async () => await PipeAppClient.TrySendAsync(
         GetServerPipeName(configurableAppSettings.LocalPluginGuid),
         $"License validation failed for Snow Proyect, please check logs for more details",
         PipeMessageType.Error
     ).ConfigureAwait(false));

            })
            .OnFail(error =>
            {
                Logger.LogError(
                    error,
                    "License validation failed in {Method}",
                    nameof(ExecuteLicensePipeline));

                _ = Task.Run(async () => await PipeAppClient.TrySendAsync(
     GetServerPipeName(configurableAppSettings.LocalPluginGuid),
     $"License validation failed for Snow Proyect, please check logs for more details",
     PipeMessageType.Error
 ).ConfigureAwait(false));

            });
    }

    /// <summary>
    /// The base name for the server named pipe used for inter-process communication.
    /// </summary>
    public const string SERVER_PIPE_NAME = "ServerAppPipe";

    /// <summary>
    /// Generates the complete server pipe name by combining the base name with the role GUID.
    /// </summary>
    /// <param name="roleGuid">The unique identifier of the role (server instance).</param>
    /// <returns>The formatted pipe name in the format "ServerAppPipe_{RoleGuidWithoutDashes}".</returns>
    /// <exception cref="ArgumentException">Thrown when roleGuid is an empty GUID.</exception>
    public static string GetServerPipeName(Guid roleGuid)
    {
        if (roleGuid == Guid.Empty)
        {
            throw new ArgumentException(
                "Role GUID cannot be empty.",
                nameof(roleGuid));
        }

        return $"{SERVER_PIPE_NAME}_{roleGuid:N}";
    }

}
