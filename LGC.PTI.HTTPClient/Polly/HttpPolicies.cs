using Microsoft.Extensions.Logging;

namespace LGC.PTI.HTTPClient.Polly;

/// <summary>
/// Provides retry policies for HTTP requests.
/// </summary>
/// <remarks>
/// The policy uses a decorrelated jitter backoff strategy and logs each retry attempt,
/// including the delay and any exception associated with the failed request.
/// </remarks>
public sealed class HttpPolicies : IFaultPolicies
{
    #region Public Fields

    /// <summary>
    /// Defines the maximum number of retry attempts.
    /// </summary>
    public const int RETRY = 3;

    #endregion Public Fields

    #region Public Methods

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPolicies"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record retry attempts and HTTP request failures.
    /// </param>
    public HttpPolicies(ILogger<HttpPolicies> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Creates an asynchronous retry policy for HTTP requests.
    /// </summary>
    /// <param name="policyBuilder">
    /// The Polly policy builder that defines which HTTP outcomes should trigger a retry.
    /// </param>
    /// <returns>
    /// An asynchronous HTTP policy configured to retry matching failures.
    /// </returns>
    /// <remarks>
    /// The policy performs up to three retries using a decorrelated jitter backoff strategy
    /// with a median first retry delay of one second. Each retry attempt is logged, and any
    /// associated exception is logged as an error.
    /// </remarks>
    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PolicyBuilder<HttpResponseMessage> policyBuilder)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(
             medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: RETRY);

        var retryPolicy = policyBuilder
     .WaitAndRetryAsync(
         delay,
         (outcome, timespan) =>
         {
             Logger.LogInformation(
                 "Resending HTTP request. Retry after {Delay}.",
                 timespan);

             if (outcome.Exception != null)
             {
                 Logger.LogError(
                     outcome.Exception,
                     "HTTP request failed.");
             }
         });
        return retryPolicy;
    }

    /// <summary>
    /// Gets the logger used for retry and HTTP request diagnostics.
    /// </summary>
    private ILogger<HttpPolicies> Logger { get; }

    #endregion Public Methods
}