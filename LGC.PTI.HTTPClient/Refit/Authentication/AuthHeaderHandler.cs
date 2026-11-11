using Microsoft.Extensions.Logging;

namespace LGC.PTI.HTTPClient.Refit;

/// <summary>
/// Adds the PTI bearer token to outgoing HTTP requests and refreshes the token
/// when the API responds with an unauthorized status.
/// </summary>
internal sealed class AuthHeaderHandler : DelegatingHandler
{
    /// <summary>
    /// Defines the HTTP authorization header name.
    /// </summary>
    private const string AUTHORIZATION_HEADER = "Authorization";

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthHeaderHandler"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used for request and authentication diagnostics.
    /// </param>
    /// <param name="authenticationSrv">
    /// The authentication service used to retrieve and refresh PTI access tokens.
    /// </param>
    /// <param name="dynamicUrlManager">
    /// The dynamic URL manager associated with PTI HTTP client configuration.
    /// </param>
    public AuthHeaderHandler(ILogger<AuthHeaderHandler> logger, IAuthenticationSrv authenticationSrv, IPTIDynamicUrlManager dynamicUrlManager)
    {
        Logger = logger;
        AuthenticationSrv = authenticationSrv;
        DynamicUrlManager = dynamicUrlManager;
    }

    #endregion Public Constructors

    #region Protected Methods

    /// <summary>
    /// Sends the specified HTTP request after applying the current PTI bearer token.
    /// </summary>
    /// <param name="request">
    /// The outgoing HTTP request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// A task that returns the HTTP response.
    /// </returns>
    /// <remarks>
    /// The request is executed through a retry policy that performs one retry when
    /// the PTI API returns <see cref="HttpStatusCode.Unauthorized"/>. Before retrying,
    /// the authentication service requests a new access token.
    /// </remarks>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var policy = CreateTokenRefreshPolicy();

        var response = await policy.ExecuteAsync(async () =>
        {
            var token = AuthenticationSrv
                .AuthTokenStore
                .TryGetToken()
                .IfNoneUnsafe(default(string));

            var authorizationHeader = $"Bearer {token}";

            request = AddOrUpdateAuthorizationHeader(
                request,
                authorizationHeader);

            Logger.LogInformation(
                "Sending PTI request. Uri: {Uri}, HasAuthorization: {HasAuthorization}, Scheme: {Scheme}",
                request.RequestUri,
                request.Headers.Authorization is not null,
                request.Headers.Authorization?.Scheme);

            return await base
                .SendAsync(request, cancellationToken)
                .IgnoreCtx();
        }).IgnoreCtx();

        return response;
    }

    /// <summary>
    /// Adds or replaces the authorization header on the specified HTTP request.
    /// </summary>
    /// <param name="request">
    /// The HTTP request whose authorization header will be updated.
    /// </param>
    /// <param name="header">
    /// The complete authorization header value.
    /// </param>
    /// <returns>
    /// The same <see cref="HttpRequestMessage"/> instance after the authorization header is updated.
    /// </returns>
    private HttpRequestMessage AddOrUpdateAuthorizationHeader(HttpRequestMessage request, string header)
    {
        if (request.Headers.Contains(AUTHORIZATION_HEADER))
        {
            request.Headers.Remove(AUTHORIZATION_HEADER);
        }

        request.Headers.Add(AUTHORIZATION_HEADER, header);
        return request;
    }

    /// <summary>
    /// Creates the retry policy used to refresh the PTI access token after an unauthorized response.
    /// </summary>
    /// <returns>
    /// An asynchronous retry policy that retries the request once after refreshing the token.
    /// </returns>
    private AsyncRetryPolicy<HttpResponseMessage> CreateTokenRefreshPolicy()
    {
        var policy = Policy
            .HandleResult<HttpResponseMessage>(message => message.StatusCode == HttpStatusCode.Unauthorized)
            .RetryAsync(1, async (result, retryCount) =>
            {
                Logger.LogInformation("refreshing Auth_key");
                _ = await AuthenticationSrv.TryGetTokenAsync().IgnoreCtx();
            });

        return policy;
    }

    #endregion Protected Methods

    #region Private Fields

    /// <summary>
    /// Gets the logger used for request and authentication diagnostics.
    /// </summary>
    private ILogger<AuthHeaderHandler> Logger { get; }

    /// <summary>
    /// Gets the authentication service used to retrieve and refresh PTI access tokens.
    /// </summary>
    private IAuthenticationSrv AuthenticationSrv { get; }

    /// <summary>
    /// Gets the dynamic URL manager associated with PTI HTTP client configuration.
    /// </summary>
    private IPTIDynamicUrlManager DynamicUrlManager { get; }

    #endregion Private Fields
}