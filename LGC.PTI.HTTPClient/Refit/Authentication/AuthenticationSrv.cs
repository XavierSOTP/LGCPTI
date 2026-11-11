using LGC.PTI.Domain.Common;

namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Provides authentication operations for obtaining and refreshing PTI access tokens.
/// </summary>
/// <remarks>
/// The service requests an access token from the configured PTI authentication endpoint,
/// stores the token in <see cref="IAuthCredentialStore"/>, and serializes token requests
/// using the configured JSON serializer settings.
/// </remarks>
public sealed class AuthenticationSrv : IAuthenticationSrv
{
    /// <summary>
    /// Gets the credential store that contains PTI authentication configuration and access tokens.
    /// </summary>
    public IAuthCredentialStore AuthTokenStore { get; }

    /// <summary>
    /// Gets the JSON serializer settings used by the Refit authentication client.
    /// </summary>
    public JsonSerializerSettings JsonSerializerSettings { get; }

    /// <summary>
    /// Gets the semaphore used to serialize token acquisition operations.
    /// </summary>
    public SemaphoreSlim Semaphore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationSrv"/> class.
    /// </summary>
    /// <param name="authTokenStore">
    /// The credential store containing the authentication endpoint, client credentials,
    /// and current access token.
    /// </param>
    /// <param name="jsonSerializerSettings">
    /// The JSON serializer settings used by the Refit authentication client.
    /// </param>
    public AuthenticationSrv(
        IAuthCredentialStore authTokenStore,
        JsonSerializerSettings jsonSerializerSettings)
    {
        AuthTokenStore = authTokenStore;
        JsonSerializerSettings = jsonSerializerSettings;
        Semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Requests a new access token from the configured PTI authentication endpoint.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous token request operation.
    /// </returns>
    /// <remarks>
    /// The existing token is cleared before the request is sent. Client credentials are encoded
    /// using HTTP Basic authentication, and the returned access token is stored in
    /// <see cref="AuthTokenStore"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the authentication endpoint returns a null or empty access token.
    /// </exception>
    private async Task RequestTokenAsync()
    {
        AuthTokenStore.ResetToken();

        var authClient = RestService.For<IPTIAuthClientApi>(
            AuthTokenStore.TokenUrl,
            new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(JsonSerializerSettings),
            });

        var clientDetails = $"{AuthTokenStore.ClientId}:{AuthTokenStore.ClientSecret}";

        var encryptedClientDetails = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(clientDetails));

        var authorizationHeader = $"Basic {encryptedClientDetails}";
        var tokenConfig = new TokenConfiguration();

        var response = await authClient.SendTokenRequest(
            authorizationheader: authorizationHeader,
            body: tokenConfig)
            .IgnoreCtx();

        var token = response.AccessToken;

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("returned token was null or empty");
        }

        AuthTokenStore.UpdateToken(token);
    }

    /// <summary>
    /// Requests a new PTI access token and returns the token currently stored in the credential store.
    /// </summary>
    /// <returns>
    /// A task that returns an <see cref="Option{T}"/> containing the access token when available;
    /// otherwise, <see cref="Option{T}.None"/>.
    /// </returns>
    /// <remarks>
    /// A semaphore ensures that only one token acquisition operation runs at a time.
    /// </remarks>
    public async Task<Option<string>> TryGetTokenAsync()
    {
        await Semaphore.WaitAsync().IgnoreCtx();

        try
        {
            await RequestTokenAsync().IgnoreCtx();

            return AuthTokenStore.TryGetToken();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Refreshes the PTI access token after authentication credentials have changed.
    /// </summary>
    /// <returns>
    /// A task that returns <see cref="Unit.Default"/> after the token refresh operation completes.
    /// </returns>
    public async Task<Unit> CredentialsChangedAsync()
    {
        await TryGetTokenAsync().IgnoreCtx();

        return Unit.Default;
    }
}