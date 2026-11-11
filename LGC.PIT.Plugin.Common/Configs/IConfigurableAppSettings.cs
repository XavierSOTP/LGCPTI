using SoftSolute.Plugin.Common.Configurations;
using System;

namespace LGC.PIT.Plugin.Common.Configs;

/// <summary>
/// Represents the configurable application settings for the PTI plugin.
/// Extends the base application settings interface with LGC-specific configuration and local plugin identification.
/// </summary>
public interface IConfigurableAppSettings : IApplicationSettings
{
    #region Properties

    /// <summary>
    /// Gets or sets the LGC-specific settings configuration.
    /// </summary>
    LGCSettings LGCSettings { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this local plugin instance.
    /// </summary>
    Guid LocalPluginGuid { get; set; }

    #endregion Properties
}
