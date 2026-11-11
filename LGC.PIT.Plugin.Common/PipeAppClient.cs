namespace LGC.PIT.Plugin.Common;

/// <summary>
/// Provides a client implementation for sending messages through named pipes.
/// </summary>
/// <remarks>
/// This class establishes an asynchronous connection to a named pipe server and sends
/// serialized <see cref="RelayEvent"/> messages. Connection failures are silently ignored
/// to prevent disruption when the pipe server is unavailable.
/// </remarks>
public sealed class PipeAppClient : IPipeAppClient
{

    /// <summary>
    /// Attempts to send a message asynchronously to the specified named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to connect to.</param>
    /// <param name="message">The message content to send.</param>
    /// <param name="messageType">The type of the message indicating its severity level.</param>
    /// <param name="memberName">
    /// The name of the calling member. This parameter is automatically populated by the compiler.
    /// </param>
    /// <param name="filePath">
    /// The full path of the source file that contains the caller. This parameter is automatically populated by the compiler.
    /// </param>
    /// <param name="lineNumber">
    /// The line number in the source file at which the method is called. This parameter is automatically populated by the compiler.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous send operation.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a <see cref="RelayEvent"/> payload containing the message, timestamp,
    /// message type, and caller context information, then serializes it to JSON before sending.
    /// </para>
    /// <para>
    /// The connection timeout is set to 300 milliseconds. If the pipe server is not available
    /// or any error occurs during the send operation, the exception is silently caught and ignored.
    /// </para>
    /// </remarks>
    public async Task TrySendAsync(string pipeName, string message, PipeMessageType messageType, [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            var className = Path.GetFileNameWithoutExtension(filePath);

            var payload = new PipeEvent
            {
                Message = message,
                Timestamp = DateTime.UtcNow,
                MessageType = messageType,
                Context = $"{className}.{memberName}.{lineNumber}",
            };

            var json = JsonConvert.SerializeObject(payload);

            await client.ConnectAsync(300).ConfigureAwait(false);

            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            await writer.WriteLineAsync(json).ConfigureAwait(false);
        }
        catch
        {
            // Ignore connection failures when the pipe server is unavailable
        }
    }
}
