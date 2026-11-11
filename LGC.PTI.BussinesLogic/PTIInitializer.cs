using Genetec.Sdk;
using LanguageExt;
using LGC.PIT.Plugin.Common.Configs;
using LGC.PTI.Domain.Infrastructure.Mapping;
using LGC.PTI.HTTPClient.Refit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using R3;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.LanguageExtSS;
using SoftSolute.Common.Utilities.Tasks;
using SoftSolute.Logging;
using SoftSolute.Plugin.Common.Configurations;
using SoftSolute.Plugin.Common.Helpers;
using SoftSolute.Plugin.Exe;
using SoftSolute.Plugin.Exe.ServiceCollections;
using SoftSolute.Sdk.Common;
using Unit = LanguageExt.Unit;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Initializes and configures the LGC PTI plugin for execution within a Genetec Security Center environment.
/// Manages dependency injection, logging configuration, and in-process service startup.
/// Handles plugin discovery, role configuration, and integration with the Genetec SDK.
/// </summary>
public sealed class PTIInitializer(
    IPluginConfiguration pluginConfiguration,
    IServiceCollection services,
    Guid roleGuid,
    string roleName)
    : PluginBaseProgram(pluginConfiguration, services)
{
    /// <summary>
    /// The role GUID for this plugin instance.
    /// </summary>
    private readonly Guid _roleGuid = ValidateRoleGuid(roleGuid);

    /// <summary>
    /// The NLog logger factory for creating logger instances.
    /// </summary>
    private readonly LogFactory _logFactory = new();

    /// <summary>
    /// The role name for this plugin instance.
    /// </summary>
    private readonly string _roleName = roleName;

    #region Fields

    /// <summary>
    /// The main service instance that manages Genetec integration and service execution.
    /// </summary>
    private MainGenetecService _mainService;

    #endregion

    /// <summary>
    /// Validates that a role GUID is not empty.
    /// </summary>
    /// <param name="roleGuid">The role GUID to validate.</param>
    /// <returns>The validated role GUID.</returns>
    /// <exception cref="ArgumentException">Thrown when the role GUID is empty.</exception>
    private static Guid ValidateRoleGuid(Guid roleGuid)
    {
        if (roleGuid == Guid.Empty)
        {
            throw new ArgumentException(
                "Role GUID cannot be empty.",
                nameof(roleGuid));
        }

        return roleGuid;
    }

    /// <summary>
    /// Starts the LGC PTI service inside the current Genetec Plugin Server process.
    /// Initializes dependency injection, configures logging, and executes the main service pipeline.
    /// </summary>
    /// <param name="pluginExeConfig">
    /// Configuration required to initialize the plugin application, including transact settings and SDK configuration.
    /// </param>
    /// <param name="engine">
    /// The Genetec engine instance provided by the Plugin Server for SDK integration.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous startup operation.
    /// </returns>
    public async Task StartInProcessAsync(LGCPluginExeConfig pluginExeConfig, IEngine engine)
    {
        var pluginRootPath =
     SetApplicationDirectory()
         .NonMonad();

        var logger = ConfigureLogging(
       pluginRootPath,
       pluginExeConfig,
       _roleName);

    

        logger.Warn(
            $"[{_roleGuid} + {_roleName}] INITIALIZER STARTING");
        try
        {
            var type = typeof(IAppInitializedNotifier);

            logger.Info(
                "Plugin.Exe assembly: {Assembly}",
                type.Assembly.FullName);

            logger.Info(
                "Plugin.Exe path: {Path}",
                type.Assembly.Location);
            logger.Info($"Host executable: {System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}");
            logger.Info($"Host configuration: {AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}");
            logger.Info("Starting LGC PTI inside the current Genetec AppDomain...");

            await ExecuteInProcessAsync(logger, pluginExeConfig, services, engine).ConfigureAwait(false);

            logger.Info("LGC PTI in-process startup completed successfully.");
        }
        catch (Exception exception)
        {
            logger.Error(exception, "ERROR FATAL: LGC PTI in-process startup failed");

            throw;
        }
    }


    /// <summary>
    /// Configures dependency injection and starts the main LGC PTI service inside the current Genetec AppDomain.
    /// Initializes all application services, registers domain modules, and executes the main service pipeline.
    /// Also performs diagnostics on assembly versioning and dependency compatibility.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="pluginExeConfig">The plugin execution configuration containing settings and transact configuration.</param>
    /// <param name="serviceCollection">The service collection for dependency injection registration.</param>
    /// <param name="engine">The Genetec engine instance for SDK integration.</param>
    /// <returns>A task representing the asynchronous configuration and execution operation.</returns>
    internal async Task ExecuteInProcessAsync(
        Logger logger,
        LGCPluginExeConfig pluginExeConfig,
        IServiceCollection serviceCollection,
        IEngine engine)
    {
        try
        {
            logger.Info("Starting dependency injection and service configuration...");


            var appFolder = SetApplicationDirectory().NonMonad();

            pluginExeConfig.TransactSettings.SetResourceFolder(appFolder);

            DirectoryHelper.EnsureDirectory(appFolder, "Databases");

            logger.Info("Starting in-process configuration...");

            var safeJson = ToSafeJson(
                pluginExeConfig,
                sensitiveFields:
                [
                    "Password",
                    "Key",
                    "Certificate",
                    "ApiSettings"
                ],
                removeSections:
                [
                    "SdkSettings",
                    "License"
                ]);

            logger.InfoAsJson(safeJson);

            var serviceConfig = CreateServiceConfig(_roleGuid, _roleName);
            var program = CreateIPluginBase(serviceConfig, serviceCollection, _roleGuid, _roleName);

            RegisterAppsettings(program, pluginExeConfig, GetSdkSettingsForPluginServer(), logger);
            RegisterLoggingDependenciesForPluginServer(
                serviceCollection,
                _logFactory); RegisterDomainTypes(program, logger);

            serviceCollection.AddSingleton<IEngine>(engine);
            serviceCollection.AddSingleton<MainGenetecService>();
            var provider = serviceCollection.BuildServiceProvider();

            _mainService = provider.GetRequiredService<MainGenetecService>();

            logger.Info("Executing RunServiceExecutionAsync...");
            var affType = typeof(LanguageExt.Aff<>).MakeGenericType(typeof(string));
            var runMethod = affType.GetMethod("Run", Type.EmptyTypes);

            var runValueTaskAssembly = runMethod
                .ReturnType
                .GetGenericTypeDefinition()
                .Assembly;

            var codeValueTaskAssembly = typeof(ValueTask<>).Assembly;

            logger.Info($"Run() ValueTask assembly: {runValueTaskAssembly.FullName}");
            logger.Info($"Run() ValueTask path: {runValueTaskAssembly.Location}");
            logger.Info($"Run() ValueTask MVID: {runValueTaskAssembly.ManifestModule.ModuleVersionId}");

            logger.Info($"Code ValueTask assembly: {codeValueTaskAssembly.FullName}");
            logger.Info($"Code ValueTask path: {codeValueTaskAssembly.Location}");
            logger.Info($"Code ValueTask MVID: {codeValueTaskAssembly.ManifestModule.ModuleVersionId}");

            logger.Info(
                $"Same ValueTask assembly instance: " +
                $"{ReferenceEquals(runValueTaskAssembly, codeValueTaskAssembly)}");

            foreach (var assembly in AppDomain.CurrentDomain
                         .GetAssemblies()
                         .Where(x =>
                             string.Equals(
                                 x.GetName().Name,
                                 "LanguageExt.Core",
                                 StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(
                                 x.GetName().Name,
                                 "System.Threading.Tasks.Extensions",
                                 StringComparison.OrdinalIgnoreCase)))
            {
                logger.Info(
                    $"Loaded assembly: {assembly.FullName} | " +
                    $"Path: {(assembly.IsDynamic ? "<dynamic>" : assembly.Location)} | " +
                    $"MVID: {assembly.ManifestModule.ModuleVersionId}");
            }

            await _mainService.RunServiceExecutionAsync().IgnoreCtx();

            logger.Info("RunServiceExecutionAsync startup call completed.");
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Error executing PTI in-process logic");

            throw;
        }
    }
    /// <summary>
    /// Loads SDK settings from the plugin server configuration located in the same directory as the executing assembly.
    /// </summary>
    /// <returns>The SDK settings loaded from the plugin server configuration file.</returns>
    private static PluginSdkSettings GetSdkSettingsForPluginServer()
    {
        var serverPath = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location);

        return LoadSdkSettings(serverPath);
    }

    /// <summary>
    /// Registers NLog logging provider dependencies for the plugin server.
    /// Configures the dependency injection container with logging support.
    /// </summary>
    /// <param name="services">The service collection to register logging dependencies into.</param>
    /// <param name="logFactory">The logger factory instance to use for creating loggers.</param>
    private static void RegisterLoggingDependenciesForPluginServer(
      IServiceCollection services,
      LogFactory logFactory)
    {
        RegisterCommonLoggingDependencies(
            services,
            loggingBuilder =>
            {
                loggingBuilder.AddProvider(
                    new PluginLoggerProvider(
                        logFactory));
            });
    }

    /// <summary>
    /// Registers domain-specific service modules into the dependency injection container.
    /// This includes configuration for app services, HTTP clients, and object mapping.
    /// </summary>
    /// <param name="program">The plugin base program instance providing service collection access.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    private static void RegisterDomainTypes(IPluginBaseProgram program, Logger logger)
    {

        program.Services.RegisterModule(new AppServiceConfiguration());
        program.Services.RegisterModule(new HtppClientModule());
        program.Services.RegisterModule(new MapsterModule());

        //program.Services.RegisterModule(new CommonModule());
        //program.Services.RegisterModule(new DynamicObserverModule(logger));

        //program.Services.RegisterModule(new MainQuartzModule());

    }

    /// <summary>
    /// Registers common logging dependencies shared by all application hosts.
    /// Configures the logging framework with a custom provider.
    /// </summary>
    /// <param name="services">The service collection to register logging dependencies into.</param>
    /// <param name="registerLoggingProvider">A callback to register the specific logging provider implementation.</param>
    private static void RegisterCommonLoggingDependencies(
     IServiceCollection services,
     Action<ILoggingBuilder> registerLoggingProvider)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();

            loggingBuilder.SetMinimumLevel(
                Microsoft.Extensions.Logging.LogLevel.Trace);

            loggingBuilder.AddFilter(
                "Microsoft.Extensions.Http.DefaultHttpClientFactory",
                Microsoft.Extensions.Logging.LogLevel.Warning);

            loggingBuilder.AddFilter(
                "System.Net.Http.HttpClient",
                Microsoft.Extensions.Logging.LogLevel.Warning);

            registerLoggingProvider(loggingBuilder);
        });
    }
    /// <summary>
    /// Registers application configuration and SDK settings into the dependency injection container.
    /// Resolves SDK and Security Center paths from environment variables and registers them for use throughout the application.
    /// This also configures the transact settings resource folder and initializes database storage.
    /// </summary>
    /// <param name="program">The plugin base program instance providing service collection access.</param>
    /// <param name="pluginAppConfig">The plugin execution configuration containing transact settings and SDK configuration.</param>
    /// <param name="sdkSettings">The SDK settings loaded from the plugin server configuration.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <returns>A Unit value representing successful completion.</returns>
    private static Unit RegisterAppsettings(IPluginBaseProgram program, LGCPluginExeConfig pluginAppConfig, PluginSdkSettings sdkSettings, Logger logger)
    {
        var appSettings = pluginAppConfig.TransactSettings;

        SetSdkSettingValues(logger, out string defaultDummy, out string _sdkLocation, out string _scLocation);

        var pluginSdkSettingsNew = new PluginSdkSettings
        {
            SdkLocation = _sdkLocation,
            SCLocation = _scLocation,
            Directory = defaultDummy,
            User = defaultDummy,
            Password = defaultDummy
        };

        var sdkSettingsNew = new SdkSettings
        {
            SdkLocation = _sdkLocation,
            SCLocation = _scLocation,
            Directory = defaultDummy,
            User = defaultDummy,
            Password = defaultDummy
        };

        pluginAppConfig.SdkSettings = pluginSdkSettingsNew;
        program.Services.AddSingleton<IConfigurableAppSettings>(appSettings);
        program.Services.AddSingleton<IApplicationSettings>(appSettings);
        program.Services.AddSingleton<ISdkSettings>(sdkSettingsNew);

        program.Services.AddSingleton<IPluginExeConfig>(pluginAppConfig);

        return Prelude.unit;

        static void SetSdkSettingValues(Logger logger, out string defaultDummy, out string _sdkLocation, out string _scLocation)
        {
            defaultDummy = "**Softsolute**";
            var scPath = Environment.GetEnvironmentVariable(AppConstants.SCPATH, EnvironmentVariableTarget.Machine);

            scPath ??= $@"C:\Program Files (x86)\Genetec Security Center {AppConstants.TARGET_VERSION}";

            _sdkLocation = $"{scPath} SDK";
            _scLocation = scPath;
            logger.Info($"Internal Value: {defaultDummy}");
            logger.Info($"SC Path: {scPath}");
            logger.Info($"SDK Location: {_sdkLocation}");
            logger.Info($"SC Location: {_scLocation}");
        }
    }

    /// <summary>
    /// Loads SDK configuration settings from the plugin server settings file.
    /// </summary>
    /// <param name="serverPath">The directory path where the plugin server is located.</param>
    /// <returns>The loaded SDK settings configuration.</returns>
    private static PluginSdkSettings LoadSdkSettings(
        string serverPath)
    {
        var serverPluginSettings =
            JsonSettings.LoadPluginSettings<PluginSdkSettings>(
                serverPath,
                "SdkSettings");

        return new PluginSdkSettings
        {
            SdkLocation = serverPluginSettings.SdkLocation,
            SCLocation = serverPluginSettings.SCLocation,
            Directory = serverPluginSettings.Directory,
            User = serverPluginSettings.User,
            Password = serverPluginSettings.Password
        };
    }

    /// <summary>
    /// Creates a new instance of the PTIInitializer as an IPluginBaseProgram.
    /// </summary>
    /// <param name="configuration">The plugin configuration to use for initialization.</param>
    /// <param name="services">The service collection for dependency injection.</param>
    /// <returns>A new PTIInitializer instance implementing IPluginBaseProgram.</returns>
    private static IPluginBaseProgram CreateIPluginBase(
     IPluginConfiguration configuration,
     IServiceCollection services,
     Guid roleGuid, string RoleName)
    {
        return new PTIInitializer(
            configuration,
            services,
            roleGuid, RoleName);
    }

    /// <summary>
    /// Creates the plugin configuration with standard LGC PTI settings.
    /// Configures the plugin organization name as "SoftSolute" and initializes the role-based configuration.
    /// </summary>
    /// <param name="roleGuid">The GUID identifying the plugin role.</param>
    /// <param name="roleName">The name of the plugin role.</param>
    /// <returns>A new PluginConfiguration instance configured for the LGC PTI plugin.</returns>
    private static IPluginConfiguration CreateServiceConfig(
    Guid roleGuid,
    string roleName)
    {
        return PluginConfiguration.Create(
            "SoftSolute",
            roleName,
            roleGuid);
    }

    /// <summary>
    /// Converts an object to a JSON representation with sensitive fields masked and specified sections removed.
    /// </summary>
    /// <param name="obj">The object to convert to JSON.</param>
    /// <param name="sensitiveFields">An array of field names to mask (replaced with "****MASKED****").</param>
    /// <param name="removeSections">An array of section names to completely remove from the output.</param>
    /// <returns>A JObject containing the sanitized JSON representation.</returns>
    public static JObject ToSafeJson(object obj, string[] sensitiveFields, string[] removeSections)
    {
        var json = JsonConvert.SerializeObject(obj);
        var jObj = JObject.Parse(json);

        MaskFields(jObj, sensitiveFields);
        RemoveSections(jObj, removeSections);

        return jObj;
    }

    /// <summary>
    /// Recursively masks sensitive fields in a JSON token tree by replacing their values with "****MASKED****".
    /// Used for safe logging of configured objects that may contain passwords or API keys.
    /// </summary>
    /// <param name="token">The JSON token to process (object, array, or scalar value).</param>
    /// <param name="sensitiveFields">An array of field names to mask with the masked value.</param>
    public static void MaskFields(JToken token, string[] sensitiveFields)
    {
        if (token.Type == JTokenType.Object)
        {
            var items = token.Children<JProperty>().ToList();

            foreach (var property in items)
            {
                if (sensitiveFields.Contains(property.Name))
                {
                    var value = property.Value?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        property.Value = "****MASKED****";
                    }
                }
                else
                {
                    MaskFields(property.Value, sensitiveFields);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var child in token.Children())
            {
                MaskFields(child, sensitiveFields);
            }
        }
    }

    /// <summary>
    /// Recursively removes specified sections from a JSON token tree.
    /// Filters out entire properties from objects at any depth in the JSON structure.
    /// </summary>
    /// <param name="token">The JSON token to process (object, array, or scalar value).</param>
    /// <param name="removeSections">An array of property names to completely remove from objects.</param>
    public static void RemoveSections(JToken token, string[] removeSections)
    {
        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>().ToList())
            {
                if (removeSections.Contains(property.Name))
                {
                    property.Remove();
                }
                else
                {
                    RemoveSections(property.Value, removeSections);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var child in token.Children())
            {
                RemoveSections(child, removeSections);
            }
        }
    }

    /// <summary>
    /// Resolves assembly dependencies by attempting to load them from the plugin folder.
    /// Used as an AssemblyResolve event handler to support custom assembly loading.
    /// </summary>
    /// <param name="sender">The sender of the resolve event.</param>
    /// <param name="args">The resolution event arguments containing the requested assembly name.</param>
    /// <returns>The loaded assembly, or null if the requested assembly cannot be found.</returns>
    public static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        try
        {
            var requestedAssembly = new AssemblyName(args.Name);
            var pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var targetPath = Path.Combine(pluginFolder, $"{requestedAssembly.Name}.dll");

            if (File.Exists(targetPath))
            {
                return Assembly.LoadFrom(targetPath);
            }
        }
        catch
        {
        }
        return null;
    }

    /// <summary>
    /// Configures and initializes the NLog logging framework for the plugin.
    /// Sets up daily log files with archive support based on the provided configuration.
    /// Logs are organized in subdirectories by role name to support multi-role plugin instances.
    /// </summary>
    /// <param name="path">The root path where log files and subdirectories will be created.</param>
    /// <param name="config">The plugin execution configuration containing logging settings and transact configuration.</param>
    /// <param name="roleName">The name of the plugin role for organizing logs into separate directories.</param>
    /// <returns>The current NLog logger instance for the calling class.</returns>
    private Logger ConfigureLogging(
      NotEmptyString path,
      LGCPluginExeConfig config,
      string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            roleName = "UnknownRole";
        }

        var safeRoleName =
            SanitizeDirectoryName(roleName);

        config.TransactSettings.ProyectRootPath =
            path;

        var configuration =
            DefaultNLogConfigurator.New()
                .CreateConfig(
                    NLog.LogLevel.Debug,
                    loggingConfiguration =>
                    {
                        DirectoryHelper.CreateDailyAndArchiveLogFile(
                            loggingConfiguration,
                            baseDirectory: path,
                            outputDirectory: $"logs/{safeRoleName}");
                    });

        _logFactory.Configuration =
            configuration;

        return _logFactory.GetLogger(
            typeof(PTIInitializer).FullName);
    }

    /// <summary>
    /// Removes invalid file name characters from a string to create a safe directory name.
    /// Replaces all invalid characters with underscores to ensure the result is a valid file path component.
    /// </summary>
    /// <param name="value">The string to sanitize for use as a directory name.</param>
    /// <returns>A sanitized string with all invalid characters replaced by underscores.</returns>
    private static string SanitizeDirectoryName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value;
    }
    /// <summary>
    /// Sets the current application directory to match the location of the executing assembly.
    /// This ensures that relative file paths resolve correctly within the plugin installation folder.
    /// Returns the application directory path wrapped in a language extension effect for functional composition.
    /// </summary>
    /// <returns>An Eff monad containing the application directory path as a NotEmptyString.</returns>
    public static Eff<NotEmptyString> SetApplicationDirectory()
    {
        return Prelude.Eff(() =>
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(path);
            return (NotEmptyString)path;
        });
    }
}