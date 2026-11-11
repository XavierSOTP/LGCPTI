using Microsoft.Extensions.Logging;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Marker interface for the plugin logger provider.
/// Identifies logger providers that are specific to the plugin architecture.
/// </summary>
public interface IPluginLoggerProvider : ILoggerProvider
{
}
