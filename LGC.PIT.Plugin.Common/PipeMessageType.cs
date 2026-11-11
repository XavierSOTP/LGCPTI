namespace LGC.PIT.Plugin.Common;

public enum PipeMessageType
{
    /// <summary>
    /// Informational message indicating normal operation or status updates.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message indicating a potential issue that does not prevent operation.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message indicating a failure or critical issue that requires attention.
    /// </summary>
    Error = 2,
    /// <summary>
    /// Recovery message indicating a successful recovery from an error.
    /// </summary>
    Recovery = 3,
    /// <summary>
    /// Critical message indicating a severe issue that requires immediate attention.
    /// </summary>
    Critical = 4

}
