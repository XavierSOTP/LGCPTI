using LGC.PTI.Domain.Common;
using Microsoft.Extensions.Logging;
using SoftSolute.Common.Utilities.EventSS;
using SoftSolute.Common.Utilities.ObjectSS;

namespace LGC.PTI.Infrastructure;

/// <summary>
/// Stores authentication credentials and access tokens used by the PTI infrastructure.
/// Provides thread-safe operations for updating credentials and tokens, and allows
/// listeners to be notified when authentication credentials change.
/// </summary>
public sealed class AuthCredentialStore : IAuthCredentialStore
{
    private string _clientSecret;
    private string _tokenUrl;
    private string _clientId;
    private string _token;

    /// <summary>
    /// Gets the event accumulator responsible for managing and notifying
    /// registered listeners when authentication credentials change.
    /// </summary>
    private EventAccumulator Accumulator { get; }

    /// <summary>
    /// Gets the logger used to record authentication credential store operations.
    /// </summary>
    private ILogger<AuthCredentialStore> Logger { get; }

    /// <summary>
    /// Gets the URL of the authentication token endpoint.
    /// </summary>
    public string TokenUrl => _tokenUrl;

    /// <summary>
    /// Gets the client identifier used for authentication.
    /// </summary>
    public string ClientId => _clientId;

    /// <summary>
    /// Gets the client secret used for authentication.
    /// </summary>
    public string ClientSecret => _clientSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthCredentialStore"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record operations performed by the credential store.
    /// </param>
    public AuthCredentialStore(
        ILogger<AuthCredentialStore> logger)
    {
        Logger = logger;

        _clientSecret = string.Empty;
        _token = string.Empty;
        _clientId = string.Empty;
        _tokenUrl = string.Empty;

        Accumulator = EventAccumulator.New(
            new IntGtZero(3),
            TimeSpan.FromSeconds(10));

        Logger.LogInformation(
            "AuthCredentialStore initialized.");
    }

    /// <summary>
    /// Clears the currently stored authentication token.
    /// </summary>
    /// <returns>
    /// A <see cref="Unit"/> value indicating that the operation completed.
    /// </returns>
    public Unit ResetToken()
    {
        ObjectHelper.SafeInterLockExchange(
            ref _token,
            string.Empty);

        Logger.LogInformation(
            "Authentication token reset.");

        return Unit.Default;
    }

    /// <summary>
    /// Attempts to retrieve the currently stored authentication token.
    /// </summary>
    /// <returns>
    /// An option containing the authentication token when a non-empty token is
    /// currently stored; otherwise, an empty option.
    /// </returns>
    public Option<string> TryGetToken()
    {
        return Prelude
            .Optional(_token)
            .KeepAsSomeIf(token =>
                token.IsNotNullOrEmpty());
    }

    /// <summary>
    /// Updates the currently stored authentication token.
    /// </summary>
    /// <param name="token">
    /// The non-empty authentication token to store.
    /// </param>
    /// <returns>
    /// A <see cref="Unit"/> value indicating that the operation completed.
    /// </returns>
    public Unit UpdateToken(
        NotEmptyString token)
    {
        ObjectHelper.SafeInterLockExchange(
            ref _token,
            token);

        Logger.LogInformation(
            "Authentication token received and stored.");

        return Unit.Default;
    }

    /// <summary>
    /// Updates the authentication credentials used to obtain access tokens and
    /// notifies all registered credential-change listeners.
    /// </summary>
    /// <param name="tokenUrl">
    /// The non-empty URL of the authentication token endpoint.
    /// </param>
    /// <param name="clientId">
    /// The non-empty client identifier used for authentication.
    /// </param>
    /// <param name="clientSecret">
    /// The non-empty client secret used for authentication.
    /// </param>
    /// <returns>
    /// A <see cref="Unit"/> value indicating that the credentials were updated.
    /// </returns>
    public Unit UpdateCredentials(
        NotEmptyString tokenUrl,
        NotEmptyString clientId,
        NotEmptyString clientSecret)
    {
        Logger.LogInformation(
            "Updating authentication credentials. TokenUrl: {TokenUrl}, ClientId: {ClientId}",
            tokenUrl,
            clientId);

        ObjectHelper.SafeInterLockExchange(
            ref _tokenUrl,
            tokenUrl);

        ObjectHelper.SafeInterLockExchange(
            ref _clientId,
            clientId);

        ObjectHelper.SafeInterLockExchange(
            ref _clientSecret,
            clientSecret);

        Logger.LogInformation(
            "Authentication credentials updated. Notifying registered listeners.");

        Accumulator.FireChangeEvent();

        return Unit.Default;
    }

    /// <summary>
    /// Registers a listener that will be invoked when authentication credentials change.
    /// </summary>
    /// <param name="listenerId">
    /// The unique non-empty identifier associated with the listener.
    /// </param>
    /// <param name="eventHandler">
    /// The asynchronous handler to execute when authentication credentials change.
    /// </param>
    /// <returns>
    /// An effect representing the listener registration operation.
    /// </returns>
    public Eff<Unit> RegisterToCredentialsChanged(
        NotEmptyString listenerId,
        Func<Task<Unit>> eventHandler)
    {
        Logger.LogInformation(
            "Registering authentication credentials listener. ListenerId: {ListenerId}",
            listenerId);

        return Accumulator.TryRegister(
            listenerId,
            eventHandler);
    }

    /// <summary>
    /// Updates the authentication credentials used to obtain access tokens and
    /// notifies all registered credential-change listeners.
    /// </summary>
    /// <param name="tokenUrl">
    /// The URL of the authentication token endpoint.
    /// </param>
    /// <param name="clientId">
    /// The client identifier used for authentication.
    /// </param>
    /// <param name="clientSecret">
    /// The client secret used for authentication.
    /// </param>
    public void UpdateCredentials(
        string tokenUrl,
        string clientId,
        string clientSecret)
    {
        Logger.LogInformation(
            "Updating authentication credentials. TokenUrl: {TokenUrl}, ClientId: {ClientId}",
            tokenUrl,
            clientId);

        ObjectHelper.SafeInterLockExchange(
            ref _tokenUrl,
            tokenUrl);

        ObjectHelper.SafeInterLockExchange(
            ref _clientId,
            clientId);

        ObjectHelper.SafeInterLockExchange(
            ref _clientSecret,
            clientSecret);

        Logger.LogInformation(
            "Authentication credentials updated. Notifying registered listeners.");

        Accumulator.FireChangeEvent();
    }
}