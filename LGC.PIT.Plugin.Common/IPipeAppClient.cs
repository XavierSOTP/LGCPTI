namespace LGC.PIT.Plugin.Common;

public interface IPipeAppClient
{
    /// <summary>
    /// Tries to send a message to the specified named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the pipe to connect to.</param>
    /// <param name="message">The message content to send.</param>
    /// <param name="messageType">The type of the message (e.g., error, info).</param>
    /// <param name="memberName">The member name of the caller.</param>
    /// <param name="filePath">The file path of the caller.</param>
    /// <param name="lineNumber">The line number of the caller.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrySendAsync(string pipeName, string message, PipeMessageType messageType, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
}
