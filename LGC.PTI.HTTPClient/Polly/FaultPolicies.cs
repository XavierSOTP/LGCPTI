using Microsoft.Extensions.Logging;

namespace LGC.PTI.HTTPClient.Polly;

/// <summary>
/// Defines fault-handling policies used for HTTP requests.
/// </summary>
public interface IFaultPolicies
{
    /// <summary>
    /// Creates a retry policy for HTTP requests using the specified Polly policy builder.
    /// </summary>
    /// <param name="policyBuilder">
    /// The Polly policy builder used to configure the retry conditions.
    /// </param>
    /// <returns>
    /// An asynchronous HTTP policy that combines retry and timeout behavior.
    /// </returns>
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PolicyBuilder<HttpResponseMessage> policyBuilder);
}

/// <summary>
/// Provides Polly-based retry and timeout policies for HTTP requests.
/// </summary>
/// <remarks>
/// The policy retries transient failures using a decorrelated jitter backoff strategy
/// and applies a timeout to each individual request attempt.
/// </remarks>
public sealed class FaultPolicies : IFaultPolicies
{
    #region Public Constructors

    /// <summary>
    /// Defines the maximum number of retry attempts.
    /// </summary>
    private const int RETRY = 3;

    /// <summary>
    /// Defines the timeout, in seconds, applied to each individual request attempt.
    /// </summary>
    private const int SINGLETIMEOUT = 30;

    /// <summary>
    /// Gets the retry policy wrapper.
    /// </summary>
    public AsyncPolicyWrap RetryPolicy { get; }

    /// <summary>
    /// Gets the logger used for fault-policy diagnostics.
    /// </summary>
    public ILogger<FaultPolicies> Logger { get; }

    /// <summary>
    /// Gets the authentication service associated with HTTP client operations.
    /// </summary>
    public IAuthenticationSrv AuthenticationSrv { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultPolicies"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used for retry and fault diagnostics.
    /// </param>
    /// <param name="authenticationSrv">
    /// The authentication service associated with HTTP client operations.
    /// </param>
    public FaultPolicies(ILogger<FaultPolicies> logger, IAuthenticationSrv authenticationSrv)
    {
        Logger = logger;
        AuthenticationSrv = authenticationSrv;
    }

    /// <summary>
    /// Creates an HTTP retry policy combined with a per-attempt timeout policy.
    /// </summary>
    /// <param name="policyBuilder">
    /// The Polly policy builder used to define the base HTTP failure conditions.
    /// </param>
    /// <returns>
    /// An asynchronous policy that retries transient HTTP failures and applies
    /// a timeout to each request attempt.
    /// </returns>
    /// <remarks>
    /// The policy retries up to three times using a decorrelated jitter backoff strategy.
    /// Retries are triggered for <see cref="HttpRequestException"/>,
    /// <see cref="TimeoutRejectedException"/>, HTTP 408 responses, and HTTP 5xx responses.
    /// Each request attempt is limited to 30 seconds using a pessimistic timeout strategy.
    /// </remarks>
    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
     PolicyBuilder<HttpResponseMessage> policyBuilder)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(1),
            retryCount: RETRY);

        var retryPolicy = policyBuilder
            .Or<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(response =>
                response.StatusCode == HttpStatusCode.RequestTimeout ||
                (int)response.StatusCode >= 500)
            .WaitAndRetryAsync(
                delay,
                async (result, timespan) =>
                {
                    Logger.LogError($"Another retry in {timespan}");

                    await Task.CompletedTask
                        .IgnoreCtx();
                });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            SINGLETIMEOUT,
            TimeoutStrategy.Pessimistic);

        return Policy.WrapAsync(
            retryPolicy,
            timeoutPolicy);
    }

    #endregion Public Constructors
}