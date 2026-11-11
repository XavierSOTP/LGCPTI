using Genetec.Sdk.Plugin.Interfaces;
using LGC.PIT.Plugin.Common;
using LGC.PIT.Plugin.Common.Configs;
using LGC.PIT.Plugin.Common.Database;
using LGC.PIT.Plugin.Common.Database.Repositories;
using LGC.PTI.App.Plugin.Server.CustomFieldServerHelper;
using LGC.PTI.BussinesLogic;
using LGC.PTI.Domain.TypeRepresentation;
using Microsoft.Extensions.DependencyInjection;
using SoftSolute.Plugin.Common.EntityDefinitions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LGC.PTI.App.Plugin.Server;

/// <summary>
/// Main plugin module class. This is the entry point of the Genetec plugin.
/// </summary>
[PluginProperty(typeof(LGCPluginDescriptor))]
public partial class PluginModule : BasePluginModule<LGCPluginConfig>, IPluginDatabaseSupport
{
    #region Fields

    private PipeServerExeAppListener _errorPipeListener;
    private ServiceProvider _serviceProvider;
    private PTIInitializer _softApplication;
    private CancellationTokenSource _appCts;
    public const string SERVER_PIPE_NAME = "ServerAppPipe";
    #endregion

    #region Public Constructors

    private void InitializeDependencyInjection()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestService, TestService>();

        _serviceProvider = services.BuildServiceProvider();

        var testService = _serviceProvider.GetRequiredService<ITestService>();

        var message = testService.GetMessage();

        Logger?.TraceInformation(message);
        ServerLog.Info(message);
    }

    /// <summary>
    /// Initializes the plugin and subscribes to assembly resolution events.
    /// </summary>
    public PluginModule()
        : base(
            moduleLogger: Logger.CreateClassLogger(typeof(PluginModule)),
            pluginLocation: Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    {
        PluginLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    private bool IsValidConfig { get; set; } = false;

    private string PluginLocation { get; }

    /// <summary>
    /// Gets the Transact database manager instance.
    /// </summary>
    public TransactDBManager _databaseManager = new TransactDBManager();

    public DatabaseManager DatabaseManager => _databaseManager;

    #endregion Public Constructors

    #region Protected Methods

    #region State Helpers

    /// <summary>
    /// Sets the plugin state to a healthy (non-warning, non-error) state with the specified message.
    /// </summary>
    /// <param name="message">The status message to display.</param>
    private void SetHealthyState(string message)
    {
        ModifyPluginState(
            new PluginStateEntry(
                AppConstants.CONFIG_CONTEXT,
                ""));

        ModifyPluginState(
            new PluginStateEntry(
                AppConstants.CONFIG_CONTEXT,
                message)
            {
                IsWarning = false,
                IsError = false
            });
    }

    #endregion

    #region Pipe Listener

    /// <summary>
    /// Starts the named pipe listener to receive messages from the external application.
    /// Processes incoming messages and updates the plugin state accordingly.
    /// </summary>
    private void StartPipeListener()
    {
        _errorPipeListener = new PipeServerExeAppListener(
            pipeName: GetServerPipeName(PluginGuid),

            onMessageAsync: async msg =>
            {
                PipeEvent evt;

                try
                {
                    evt = JsonConvert.DeserializeObject<PipeEvent>(msg);
                }
                catch
                {
                    evt = new PipeEvent
                    {
                        Message = msg,
                        MessageType = PipeMessageType.Error
                    };
                }

                ServerLog.Warn(
                    $"PIPE MESSAGE RECEIVED | Type: {evt.MessageType} | Message: {evt.Message}");

                switch (evt.MessageType)
                {
                    case PipeMessageType.Info:
                        SetHealthyState(evt.Message);
                        break;

                    case PipeMessageType.Warning:
                        Logger?.TraceWarning(evt.Message);

                        ServerLog.Warn(
                            $"{AppConstants.MESSAGE_FROM_APPLICATION} {evt.Message}");

                        ModifyPluginState(
                            PluginStateFactory.CreateWarning(
                                AppConstants.CONFIG_CONTEXT,
                                evt.Message));

                        break;

                    case PipeMessageType.Error:
                        Logger?.TraceError(evt.Message);

                        ServerLog.Error(
                            $"{AppConstants.MESSAGE_FROM_APPLICATION} {evt.Message}");

                        ModifyPluginState(
                            PluginStateFactory.CreateError(
                                AppConstants.CONFIG_CONTEXT,
                                evt.Message));

                        ServerLog.Error(
                            "Starting: Stopped application due to error.");

                        //StopExternalApplication();

                        ServerLog.Error(
                            "Completed: Stopped application due to error.");

                        break;

                    case PipeMessageType.Recovery:
                        ModifyPluginState(
                            PluginStateFactory.CreateWarning(
                                AppConstants.CONFIG_CONTEXT,
                                "Recovery requested. Restarting external application."));

                        ServerLog.Error(
                            "Restarting external application.");

                        RestartExternalApplication();

                        break;
                }

                await Task.CompletedTask;
            });

        _errorPipeListener.Start();
    }

    #endregion

    /// <summary>
    /// Releases the unmanaged resources used by the plugin and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        try
        {
            var roleIdentity = GetRoleIdentity();

            ServerLog.Warn(
                $"[{roleIdentity.LogId}] PLUGIN DISPOSING | " +
                $"Role: {roleIdentity.RoleName} | " +
                $"PID: {Process.GetCurrentProcess().Id}");

            Engine.EntitiesInvalidated -= EngineOnEntitiesInvalidated;

            _appCts?.Cancel();
            _appCts?.Dispose();
            _appCts = null;

            _errorPipeListener?.StopAsync().GetAwaiter().GetResult();
            _errorPipeListener = null;

            _serviceProvider?.Dispose();
            _serviceProvider = null;
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Called when the plugin is loaded. Initializes components and starts external processes.
    /// </summary>
    protected override void OnPluginLoaded()
    {

        LoggingBootstrapper.Init();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            ServerLog.Error(
                $"UNHANDLED EXCEPTION | Terminating: {args.IsTerminating} | Exception: {args.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ServerLog.Error(
                $"UNOBSERVED TASK EXCEPTION | Exception: {args.Exception}");

            args.SetObserved();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            ServerLog.Error(
                $"PROCESS EXIT | PID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
        };

        var roleIdentity = GetRoleIdentity();

        ServerLog.Warn(
            $"[{roleIdentity.LogId}] PLUGIN LOADED | " +
            $"Role: {roleIdentity.RoleName} | " +
            $"PID: {Process.GetCurrentProcess().Id}");

        InitializeDependencyInjection();

        ModifyPluginState(
            PluginStateFactory.CreateWarning(
                AppConstants.CONFIG_CONTEXT,
                "Plugin is starting"));

        InitializePluginConfiguration2(
            defaultConfiguration: LGCPluginConfig.GetDefaultConfig());

        //EnsurePluginConfigIsValid();

        Engine.EntitiesInvalidated += EngineOnEntitiesInvalidated;
    }


    protected SoftSolute.Plugin.Common.LanguageExt.Unit InitializePluginConfiguration2(
 LGCPluginConfig defaultConfiguration)
    {
        var role =
            base.Engine.GetEntity<Role>(
                base.PluginGuid);

        ServerLog.Info(
            $"ROLE CONFIG | " +
            $"RoleGuid: {base.PluginGuid} | " +
            $"SpecificConfiguration: {role?.SpecificConfiguration}");

        if (string.IsNullOrWhiteSpace(role?.SpecificConfiguration))
        {
            PluginConfig =
                defaultConfiguration;

            ModuleLogger.TraceWarning(
                "Plugin configuration was null or empty");
        }
        else
        {
            ReloadPluginConfiguration2();

            ServerLog.Info(
                $"CONFIG DESERIALIZED | " +
                $"RoleGuid: {base.PluginGuid} | " +
                $"CardholderGroupId: {PluginConfig.CardholderGroupId}");
        }

        return SoftSolute.Plugin.Common.LanguageExt.Unit.Default;
    }

    protected void ReloadPluginConfiguration2()
    {
        Role entity = base.Engine.GetEntity<Role>(base.PluginGuid);
        if (string.IsNullOrWhiteSpace(entity?.SpecificConfiguration))
        {
            ModuleLogger.TraceWarning("Plugin configuration was null or empty, you must initialize the plugin configuration");
            return;
        }

        PluginConfig = SerializationHelper.DeserializeFromString<LGCPluginConfig>(entity.SpecificConfiguration);
        ModuleLogger.TraceInformation("Plugin configuration was loaded successfully");
    }
    /// <summary>
    /// Called after plugin loading. Starts operational.
    /// </summary>
    protected override void OnPluginStart()
    {
        //System.Diagnostics.Debugger.Launch();

        //var restored = TryRestoreConfigFromDatabase();

        //if (!restored)
        //{
        //    SaveConfigToDatabase();
        //}

        StartPipeListener();
        VerifyAndCreateCustomFields();
        StartInProcessApplication();

        SetHealthyState("Plugin is up and running");
    }

    /// <summary>
    /// Verifies and creates required custom fields for the plugin.
    /// </summary>
    /// <remarks>
    /// If a security level custom field is already selected in the configuration, this method returns early.
    /// Otherwise, it creates the required custom fields and auto-sets them in the configuration.
    /// </remarks>
    private void VerifyAndCreateCustomFields()
    {
        try
        {           
            var requiredFields = CreateCustomFieldList();

            if (requiredFields.Count > 0)
            {
                CustomFieldBatchOpsVerify
                    .EnsureCustomFieldsAsync(Engine, requiredFields)
                    .GetAwaiter().GetResult();

                //AutoSetCustomFiedls();
            }
        }
        catch (Exception ex)
        {
            ServerLog.Error(ex, "Error Creating Custom Fields");
            ModifyPluginState(PluginStateFactory.CreateError(AppConstants.CONFIG_CONTEXT, ex.Message));
        }
    }

    /// <summary>
    /// Automatically sets the security level custom field in the plugin configuration
    /// by retrieving it from the cardholder custom fields.
    /// </summary>
    private void AutoSetCustomFiedls()
    {
        var customFieldCardholder = base.Engine.GetCustomFields(EntityType.Cardholder);
        var cardholderDefinition = CardholderDefinition.New().Populate(customFieldCardholder);

        var securityLevelCustomfield = customFieldCardholder.FirstOrDefault(cf => cf.Name == cardholderDefinition.Department.Name);

        if (securityLevelCustomfield != null)
        {
            PluginConfig.CardholderGroupId = securityLevelCustomfield.Guid;
            ReloadPluginConfiguration();
        }
    }

    /// <summary>
    /// Creates the list of custom fields that need to be created for the plugin.
    /// </summary>
    /// <returns>A list of <see cref="CustomFieldDataBO"/> objects representing the custom fields to create.</returns>
    /// <remarks>
    /// The security level custom field is only added if the environment variable "CredVal_CFSysLevel" is set.
    /// </remarks>
    private static List<CustomFieldDataBO> CreateCustomFieldList()
    {
        var CustomFieldsToCreate = new List<CustomFieldDataBO>();
        var cardholderDef = new CardholderDefinition();

        var cardholderDefCred = new CredentialDefinition();
      
        CustomFieldsToCreate.Add(cardholderDef.Department);
        CustomFieldsToCreate.Add(cardholderDefCred.IsUsedCredential);
        
        return CustomFieldsToCreate;
    }

    private bool TryRestoreConfigFromDatabase()
    {
        try
        {
            var sqlExecutor = new SqlExecutor(_databaseManager.GetConnectionString());
            var configurationRepository = new ConfigurationRepository(sqlExecutor);

            // creo que la logica seria esta:
            // Si configuracion entrante (role) o pantalla es correcta entonces
            //      -> Ignoramos la base de datos, usamos la configuracion entrante y actualizamos el registro en la base de datos con el metodo Upsert del repositorio.

            // Si configuracion entrante (role) o pantalla es nulla o esta vacia, Entonces
            //      -> Intentamos leer la configuracion de la base de datos.
            // si la configuracion de la base de datos es correcta, entonces reemplazamos la configuracion de entrante, y guardamos la configuracion en el Role.
            // Si la configuracion de la base de datos es incorrecta, entonces usamos la configuracion por defecto y que el cliente llene los campos.

            // NOTA IMPORTANTE: Hay que buscar el metodo de Apply Changes donde el client guarda la configuracion del role para que tambien ahi
            // llamemos el metodo de actualizar la base de datos, de esta manera la configuracion de la base de datos siempre estara sincronizada con la configuracion del role.




            //var configResult = configurationRepository
            //    .GetAllAsync(c => c.Key == nameof(LGCPluginConfig))
            //    .GetAwaiter()
            //    .GetResult();


            var config = configurationRepository.GetByKeyAsync(nameof(LGCPluginConfig))
                .GetAwaiter()
                .GetResult();

            if (config == null)
            {
                Logger?.TraceWarning("No existing configuration found in database. This appears to be the first run. Default configuration will be used.");

                return false;
            }

            var deserializedConfig = SerializationHelper.DeserializeFromString<LGCPluginConfig>(config.Value);

            PluginConfig = deserializedConfig;

            return true;
        }
        catch (Exception ex)
        {
            Logger?.TraceWarning(
                $"DB restore failed, using default config. Reason: {ex.Message}");

            return false;
        }
    }
    private void SaveConfigToDatabase()
    {
        try
        {
            var sqlExecutor = new SqlExecutor(
                _databaseManager.GetConnectionString());

            var configurationRepository = new ConfigurationRepository(
                sqlExecutor);

            var serialized =
                SerializationHelper.SerializeToString(PluginConfig);

            configurationRepository
                .UpsertAsync(
                    nameof(LGCPluginConfig),
                    serialized)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Logger?.TraceWarning(
                $"DB save failed. Reason: {ex.Message}");
        }
    }

    private Aff<string> TestMethod()
    {
        return Prelude.Aff<string>(
            () => new ValueTask<string>("test"));
    }

    private bool EnsurePluginConfigIsValid()
    {
        var isValid =
            PluginConfig.IsPluginConfigValid(out var validationErrors);

        IsValidConfig = isValid;

        if (!isValid)
        {
            var humanized = validationErrors
                .Select(e => e?.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e =>
                    e.EndsWith(".")
                        ? e.Substring(0, e.Length - 1)
                        : e)
                .Select(e =>
                    char.ToUpper(e[0]) + e.Substring(1))
                .ToList();

            ModifyPluginState(PluginStateFactory.CreateError(AppConstants.CONFIG_CONTEXT,
                    $"Configuration incomplete. Please complete via Config Tool: {string.Join(", ", humanized)}"));

            Logger?.TraceWarning("❌ Plugin configuration is incomplete. External app will not be launched.");

            foreach (var error in validationErrors)
            {
                Logger?.TraceWarning($"⚠️ Validation error: {error}");
            }

            throw new ArgumentException();
        }

        return isValid;
    }

    private (Guid RoleGuid, string RoleName, string LogId) GetRoleIdentity()
    {
        if (PluginGuid == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The Genetec plugin Role GUID is empty.");
        }

        var role = Engine.GetEntity<Role>(PluginGuid);

        var roleName = role?.Name;

        if (string.IsNullOrWhiteSpace(roleName))
        {
            roleName = "UnknownRole";
        }

        var logId = PluginGuid
            .ToString("N")
            .Substring(0, 4)
            .ToUpperInvariant();

        return (
            PluginGuid,
            roleName,
            logId);
    }

    private void HandleExternalProcessExit(
        object _,
        EventArgs e)
    {
        Logger?.TraceWarning(
            "⚠️ .exe app was closed by an outside source..");

        ModifyPluginState(
            PluginStateFactory.CreateError(
                AppConstants.CONFIG_CONTEXT,
                "❌ .exe ended by an outside source"));
    }

    private void StartInProcessApplication()
    {
        var roleIdentity = GetRoleIdentity();

        ServerLog.Info(
            $"[{roleIdentity.LogId}] Starting plugin instance | " +
            $"Role: {roleIdentity.RoleName}");

        EnsurePluginConfigIsValid();


        try
        {
            var appConfig = PluginConfigMapper.ToAppConfig(
                PluginConfig,
                Engine,
                PluginGuid,
                PluginLocation,
                SdkClientCertificate);

            appConfig.TransactSettings.ConnectionString =
                _databaseManager.GetConnectionString();

            ServerLog.Info(
                appConfig.ToString());

            _appCts = new CancellationTokenSource();

            Task.Run(
                async () =>
                {
                    try
                    {
                        var serviceCollection =
                            new ServiceCollection();

                        var pluginConfig =
                            CreateServiceConfig();

                        ServerLog.Info(
                            "Starting LGC Application");

                        _softApplication = new PTIInitializer(
                          pluginConfig,
                          serviceCollection,
                          roleIdentity.RoleGuid,
                          roleIdentity.RoleName);


                        await _softApplication.StartInProcessAsync(
                            appConfig,
                            Engine);

                        SetHealthyState(
                            "✅ In-process app started successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger?.TraceError(
                            ex,
                            "Error starting in-process app");

                        ModifyPluginState(
                            PluginStateFactory.CreateError(
                                AppConstants.CONFIG_CONTEXT,
                                $"❌ Error executing logic: {ex.Message}"));
                    }
                },
                _appCts.Token);
        }
        catch (Exception ex)
        {
            ServerLog.Info(
                "Error Starting App" + ex);

            ModifyPluginState(
                PluginStateFactory.CreateError(
                    AppConstants.CONFIG_CONTEXT,
                    $"❌ Error initializing app: {ex.Message}"));
        }
    }

    private PluginConfiguration CreateServiceConfig()
    {
        var roleIdentity = GetRoleIdentity();

        return PluginConfiguration.Create(
            roleIdentity.RoleName,
            "Softsolute PTI QSL Plugin",
            roleIdentity.RoleGuid);
    }

    #endregion Protected Methods

    #region Private Methods

    private static bool IsRestartRequired(
        LGCPluginConfig oldConfig,
        LGCPluginConfig newConfig)
    {
        return false;

        //!/*Equals(oldConfig.CronExpression, newConfig.CronExpression);*/
    }

    private void EngineOnEntitiesInvalidated(
        object sender,
        EntitiesInvalidatedEventArgs e)
    {
        if (e.IsLocalUpdate)
        {
            return;
        }

        //var updated = e.Entities.FirstOrDefault(x => x.EntityGuid == PluginGuid);
        //if (updated != null)
        //{
        //    var previousConfig = PluginConfig.Clone();
        //    ReloadPluginConfiguration();
        //    SaveConfigToDatabase();

        //    var newConfig = PluginConfig;

        //    if (EnsurePluginConfigIsValid() &&
        //        IsRestartRequired(previousConfig, newConfig))
        //    {
        //        Logger?.TraceWarning(
        //            "Critical configuration Modified, restarting .Exe..");

        //        RestartExternalApplication();
        //    }
        //}
    }

    /// <summary>
    /// Triggered when entities are updated. Used to reload plugin configuration.
    /// </summary>
    private void RestartExternalApplication()
    {
        //StopExternalApplication();

        StartInProcessApplication();
    }
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

    #endregion Private Methods
}

public interface ITestService
{
    string GetMessage();
}

public sealed class TestService : ITestService
{
    public string GetMessage()
    {
        return "Microsoft Dependency Injection is working inside the Genetec plugin.";
    }
}