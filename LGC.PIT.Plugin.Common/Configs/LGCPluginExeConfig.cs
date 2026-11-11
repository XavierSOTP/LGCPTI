using SoftSolute.Plugin.Common.Configurations;

namespace LGC.PIT.Plugin.Common.Configs;

public sealed class LGCPluginExeConfig : PluginExeConfigBase, IPluginExeConfig<LGCExeSettings>
{
    public LGCExeSettings TransactSettings { get; set; }
    LGCExeSettings IPluginExeConfig<LGCExeSettings>.TransactSettings
    {
        get => TransactSettings;
        set => TransactSettings = value;
    }
}
