namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Provides a test HTTP message handler that adds a configured API key
/// to outgoing requests using the <c>Authentication</c> header.
/// </summary>
internal sealed class TestHandler : DelegatingHandler
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHandler"/> class.
    /// </summary>
    /// <param name="apiKey">
    /// The API key added to outgoing HTTP requests.
    /// </param>
    public TestHandler(string apiKey)
    {
        InnerHandler = new HttpClientHandler();
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets the API key added to outgoing HTTP requests.
    /// </summary>
    private string ApiKey { get; }

    #endregion Public Constructors

    #region Protected Methods

    /// <summary>
    /// Sends the specified HTTP request after adding the configured API key
    /// to the <c>Authentication</c> header.
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
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("Authentication", ApiKey);
        var response = await base.SendAsync(request, cancellationToken).IgnoreCtx();
        return response;
    }

    #endregion Protected Methods
}