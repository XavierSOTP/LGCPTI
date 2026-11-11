namespace LGC.PIT.Plugin.Common;

public sealed class PipeServerExeAppListener : IDisposable
{
    /// <summary>
    /// The name of the named pipe to listen on.
    /// </summary>
    private readonly string _pipeName;

    /// <summary>
    /// The callback delegate invoked when a message is received.
    /// </summary>
    private readonly Func<string, Task> _onMessageAsync;

    /// <summary>
    /// The cancellation token source used to stop the listening loop.
    /// </summary>
    private CancellationTokenSource? _cts;

    /// <summary>
    /// The task representing the active listening loop.
    /// </summary>
    private Task? _loopTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeServerExeAppListener"/> class.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to listen on.</param>
    /// <param name="onMessageAsync">
    /// The asynchronous callback delegate invoked when a message is received from a client.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pipeName"/> or <paramref name="onMessageAsync"/> is <see langword="null"/>.
    /// </exception>
    public PipeServerExeAppListener(string pipeName, Func<string, Task> onMessageAsync)
    {
        _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        _onMessageAsync = onMessageAsync ?? throw new ArgumentNullException(nameof(onMessageAsync));
    }

    /// <summary>
    /// Starts the named pipe server and begins listening for incoming connections.
    /// </summary>
    /// <remarks>
    /// This method is idempotent; calling it multiple times has no effect if the server is already running.
    /// The listening loop runs on a background thread.
    /// </remarks>
    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Stops the named pipe server and waits for the listening loop to complete.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous stop operation.</returns>
    /// <remarks>
    /// This method cancels the listening loop and waits for it to terminate gracefully.
    /// Any exceptions during shutdown are silently ignored.
    /// </remarks>
    public async Task StopAsync()
    {
        if (_cts == null) return;

        _cts.Cancel();

        try
        {
            if (_loopTask != null)
                await _loopTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore cancellation or errors from the loop during stop
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _loopTask = null;
        }
    }

    /// <summary>
    /// Continuously listens for incoming pipe client connections until cancellation is requested.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for stop requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous listening operation.</returns>
    /// <remarks>
    /// On connection failure, the method waits 250 milliseconds before retrying to prevent
    /// tight error loops.
    /// </remarks>
    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.In,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                await ReadClientAsync(server, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // If something fails, restart the server for subsequent messages
                await Task.Delay(250, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads and processes messages from a connected pipe client.
    /// </summary>
    /// <param name="pipe">The connected named pipe server stream.</param>
    /// <param name="ct">The cancellation token to monitor for stop requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous read operation.</returns>
    /// <remarks>
    /// Messages are read line by line using UTF-8 encoding. Errors in the message handler
    /// callback are silently caught to prevent listener termination.
    /// </remarks>
    private async Task ReadClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using var reader = new StreamReader(pipe, Encoding.UTF8);

        while (!ct.IsCancellationRequested && pipe.IsConnected)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line == null) break;

            try
            {
                await _onMessageAsync(line).ConfigureAwait(false);
            }
            catch
            {
                // Prevent handler errors from crashing the listener
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="PipeServerExeAppListener"/>.
    /// </summary>
    /// <remarks>
    /// This method initiates an asynchronous stop but does not wait for completion.
    /// For graceful shutdown, call <see cref="StopAsync"/> before disposing.
    /// </remarks>
    public void Dispose()
    {
        _ = StopAsync();
    }
}
