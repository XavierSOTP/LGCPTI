using System.Windows.Controls;
using GenetecPages = Genetec.Sdk.Workspace.Pages;
using SoftSolute.Plugin.Common.LanguageExt;

namespace LGC.PTI.Plugin.Client.ConfigPages.Plugin.General;

/// <summary>
/// Represents the configuration page for the Sample Plugin inside Security Center Config Tool.
/// This page allows editing and persisting of PluginConfig settings at the Role level.
/// </summary>
public abstract class PluginConfigPageBase<TSelf, TConfig, TView> : GenetecPages.ConfigPage
    where TSelf : PluginConfigPageBase<TSelf, TConfig, TView>, new()
    where TConfig : class, new()
    where TView : UserControl, new()
{
    #region Fields

    public Guid PlugingType { get; }
    protected Logger Logger { get; } = Logger.CreateClassLogger(typeof(TSelf));
    protected TConfig PluginConfig { get; set; }
    protected Guid PluginGuid { get; set; }

    #endregion Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigPage"/> class.
    /// </summary>
    protected PluginConfigPageBase(NotEmptyGuid plugingType)
    {
        View = new TView { DataContext = this };
        PluginConfig = new TConfig();
        PlugingType = plugingType;
    }

    #endregion Public Constructors

    #region Properties

    /// <summary>
    /// Sets the current entity (Role) and determines visibility based on the plugin subtype.
    /// </summary>
    protected override Guid Entity
    {
        set
        {
            var selectedEntity = Workspace.Sdk.GetEntity(value) as Role;
            PluginGuid = value;
            IsVisible = selectedEntity?.SubType == PlugingType;
        }
    }

    /// <summary>
    /// Gets the EntityType this config page is applicable to.
    /// </summary>
    protected override EntityType EntityType => EntityType.Role;

    #endregion Properties

    #region Protected Methods

    protected void ProcessRoleUpdate(IEnumerable<EntityUpdateInfo> entitiesUpdated)
    {
        foreach (var entity in entitiesUpdated)
        {
            // si es local y es tipo role y es el guid de este role  hacemos refresh
            if (entity.EntityType == EntityType.Role && entity.EntityGuid == PluginGuid)
            {
                Refresh();
            }
        }
    }

    /// <summary>
    /// Loads configuration from the role and updates UI bindings.
    /// </summary>
    protected void RefreshImpl()
    {
        var pluginRole = Workspace.Sdk.GetEntity(PluginGuid) as Role;
        if (pluginRole == null) return;

        PluginConfig = string.IsNullOrWhiteSpace(pluginRole.SpecificConfiguration)
            ? new TConfig()
            : SerializationHelper.DeserializeFromString<TConfig>(pluginRole.SpecificConfiguration);

        Logger.TraceDebug($"{Name} page refreshed for: {PluginGuid}");
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Saves the current configuration to the role.
    /// </summary>
    protected void SaveImpl()
    {
        var pluginRole = Workspace.Sdk.GetEntity(PluginGuid) as Role;
        if (pluginRole == null) return;

        pluginRole.SpecificConfiguration = SerializationHelper.SerializeToString(PluginConfig);
    }

    #endregion Protected Methods
}