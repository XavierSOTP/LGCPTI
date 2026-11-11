namespace LGC.PIT.Plugin.Common;

public static class Globals
{
    #region Fields

#warning Assign new GUIDs for your plugin
    public const string PLUGIN_COMM_EVENT = "LGC PIT Plugin CE";
    public const string PLUGIN_NAME = "LGC PIT Plugin";
    public static readonly Guid PluginTypeGuid = new("{2B7F9C41-6D8A-4E35-A2C7-91F0B4D6E853}");

    #endregion Fields

    #region Properties

    public const string DESCRIPTION = "Plugin to manage LGC PIT in Genetec Security Center.";
    public static Guid CanViewSensorCustomTask => new("{D84E7B2A-19C5-4F63-8A71-2E9C5D0B4F26}");
    public static Guid GetSensorsNumberQuery => new("{6F3A1C98-B742-4D5E-91A0-7C2E8B4F36D1}");
    public static Guid SensorCustomPageId => new("{A27D5E64-8B31-4C9F-B206-5E7A1D3C84F9}");
    public static Guid SensorReportGuid => new("{C95B2F71-4E68-43DA-87C1-9A6D3F20B5E4}");

    #endregion Properties
}