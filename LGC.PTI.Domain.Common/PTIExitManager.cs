using SoftSolute.Common.Utilities;

namespace LGC.PTI;

/// <summary>
/// Provides application exit management for the PTI integration.
/// </summary>
/// <remarks>
/// Inherits the exit-management behavior from <see cref="AppExitManager{T}"/>
/// and exposes a PTI-specific manager type.
/// </remarks>
public sealed class PTIExitManager : AppExitManager<PTIExitManager>
{
}