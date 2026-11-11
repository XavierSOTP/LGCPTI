using SoftSolute.Plugin.Common.Configurations;
using System;

namespace LGC.PIT.Plugin.Common.Configs;

/// <summary>
/// Represents the configurable application settings.
/// </summary>
public sealed class LGCExeSettings : BaseExecutableSettings, IConfigurableAppSettings
{
    public Guid LocalPluginGuid { get; set; }
    public string ConnectionString { get; set; }
    public string ProyectRootPath { get; set; }
    public string PluginFolderPath { get; set; }
    public LGCSettings LGCSettings { get; set; }
}
