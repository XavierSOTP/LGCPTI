using ConfigPage = LGC.PTI.Plugin.Client.ConfigPages.Plugin.General.ConfigPage;
using Module = Genetec.Sdk.Workspace.Modules.Module;

namespace LGC.PTI.Plugin.Client;

/// <summary>
/// Represents the Genetec client module responsible for registering and managing
/// the PTI plugin configuration page within Config Tool.
/// </summary>
public class PluginClientModule : Module
{
    #region Fields

    private ConfigPage _configPage;

    #endregion Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginClientModule"/> class
    /// and registers the assembly resolution handler.
    /// </summary>
    public PluginClientModule()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnCurrentDomainAssemblyResolve;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Loads the plugin client module.
    /// When running inside Config Tool, initializes and registers the plugin
    /// configuration page with the Genetec configuration service.
    /// </summary>
    public override void Load()
    {
        if (Workspace.ApplicationType == ApplicationType.ConfigTool)
        {
            _configPage = new ConfigPage();
            _configPage.Initialize(Workspace);
            var configurationService = Workspace.Services.Get<IConfigurationService>();
            configurationService?.Register(_configPage);
        }
    }

    /// <summary>
    /// Unloads the plugin client module by unregistering the configuration page
    /// and removing the assembly resolution handler.
    /// </summary>
    public override void Unload()
    {
        if (_configPage != null)
        {
            var configurationService =
                Workspace.Services.Get<IConfigurationService>();

            configurationService?.Unregister(
                _configPage);

            _configPage = null;
        }

        AppDomain.CurrentDomain.AssemblyResolve -=
            OnCurrentDomainAssemblyResolve;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Attempts to resolve an assembly dependency from the directory containing
    /// the currently executing plugin assembly.
    /// </summary>
    /// <param name="sender">
    /// The source of the assembly resolution event.
    /// </param>
    /// <param name="args">
    /// The event arguments containing information about the assembly that
    /// could not be resolved.
    /// </param>
    /// <returns>
    /// The resolved <see cref="Assembly"/> when the dependency is found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    private Assembly OnCurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
    {
#warning Use the Assemblyloader class
        try
        {
            var serverModulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var length = args.Name.Contains(",") ? args.Name.IndexOf(",", StringComparison.Ordinal) : args.Name.Length;
            if (serverModulePath != null)
            {
                var path = Path.Combine(serverModulePath, args.Name.Substring(0, length) + ".dll");
                if (File.Exists(path))
                {
                    var myAssembly = Assembly.LoadFrom(path);
                    return myAssembly;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    #endregion Private Methods
}