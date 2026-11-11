namespace LGC.PIT.Plugin.Common;

public sealed class PipeEvent
{
    #region Properties

    /// <summary>
    /// Gets or sets the context in which the error occurred.
    /// </summary>
    public string Context { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the source of the error (e.g., "Plugin" or "Exe").
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the stack trace of the error.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the type of relay message.
    /// </summary>
    public PipeMessageType MessageType { get; set; }

    #endregion Properties
}