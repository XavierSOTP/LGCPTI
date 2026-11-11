using LGC.PIT.Plugin.Common;
using LGC.PIT.Plugin.Common.Configs;
using System.Drawing;

namespace LGC.PTI.App.Plugin.Server;


/// <summary>
/// To force a 32-bit process, also override Supports64Bits and set it to false
/// </summary>
public sealed class LGCPluginDescriptor : PluginDescriptor
{
    #region Properties

    /// <summary>
    /// Description in the Identity page of the plugin in the Config Tool
    /// </summary>
    public override string Description => Globals.DESCRIPTION;

    /// <summary>
    /// Name displayed by default in the Config Tool when added
    /// </summary>
    public override string Name => Globals.PLUGIN_NAME;

    /// <summary>
    /// Used to uniquely identify your plugin in the system. Do NOT copy paste across projects !
    /// Do NOT change once the plugin is deployed
    /// </summary>
    public override Guid PluginGuid => Globals.PluginTypeGuid;

    /// <summary>
    /// Configuration of your plugin saved in the Directory database. Usually this will be a serialized object
    /// </summary>
    public override string SpecificDefaultConfig => SerializationHelper.SerializeToString(LGCPluginConfig.GetDefaultConfig());

    public override Bitmap SmallImage => LoadBitmap("LGC.PTI.App.Plugin.Server.Resources.icon64.png");

    public override Bitmap LargeImage => LoadBitmap("LGC.PTI.App.Plugin.Server.Resources.icon256.png");

    private static Bitmap LoadBitmap(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        return stream != null ? new Bitmap(stream) : null;
    }

    #endregion Properties
}