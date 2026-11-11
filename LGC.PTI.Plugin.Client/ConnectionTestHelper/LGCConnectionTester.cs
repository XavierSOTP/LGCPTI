using LGC.PIT.Plugin.Common.Configs;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LGC.PTI.Plugin.Client;

/// <summary>
/// Represents the result of an LGC API connection test.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection test completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message describing the result of the connection test.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Provides functionality for testing connectivity and authentication
/// against the LGC API token endpoint.
/// </summary>
public static class LGCConnectionTester
{
    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    /// <summary>
    /// Tests the configured LGC API credentials by requesting an access token.
    /// </summary>
    /// <param name="tokenUrl">
    /// The base URL of the authentication service.
    /// </param>
    /// <param name="clientId">
    /// The client identifier used for OAuth authentication.
    /// </param>
    /// <param name="clientSecret">
    /// The client secret used for OAuth authentication.
    /// </param>
    /// <returns>
    /// A task containing the result of the connection test.
    /// </returns>
    public static async Task<ConnectionTestResult> TestAsync(
        string tokenUrl,
        string clientId,
        string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(tokenUrl) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return Fail("Fill in all LGC API Security fields before testing.");
        }

        try
        {
            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type",    AppConstants.DEFAULT_GRANT_TYPE),
                new KeyValuePair<string, string>("client_id",     clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var finalTokenUrl = $"{tokenUrl.TrimEnd('/')}/token";

            var request = new HttpRequestMessage(HttpMethod.Post, finalTokenUrl) { Content = body };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _client.SendAsync(request);
            var body2 = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && body2.Contains("access_token"))
                return Ok("Connection successful. Token obtained from iqtechportal.");

            return Fail(ResolveError(response.StatusCode, body2));
        }
        catch (TaskCanceledException)
        {
            return Fail("Timed out (15s). Verify the Service URL and network connectivity.");
        }
        catch (HttpRequestException ex)
        {
            return Fail($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a successful connection test result.
    /// </summary>
    /// <param name="msg">
    /// The message associated with the successful result.
    /// </param>
    /// <returns>
    /// A successful <see cref="ConnectionTestResult"/>.
    /// </returns>
    private static ConnectionTestResult Ok(string msg) => new() { Success = true, Message = msg };

    /// <summary>
    /// Creates a failed connection test result.
    /// </summary>
    /// <param name="msg">
    /// The message describing the failure.
    /// </param>
    /// <returns>
    /// A failed <see cref="ConnectionTestResult"/>.
    /// </returns>
    private static ConnectionTestResult Fail(string msg) => new() { Success = false, Message = msg };

    /// <summary>
    /// Resolves an HTTP response into a user-friendly connection error message.
    /// </summary>
    /// <param name="code">
    /// The HTTP status code returned by the authentication endpoint.
    /// </param>
    /// <param name="body">
    /// The response body returned by the authentication endpoint.
    /// </param>
    /// <returns>
    /// A user-friendly description of the connection or authentication error.
    /// </returns>
    private static string ResolveError(System.Net.HttpStatusCode code, string body)
    {
        if (body.Contains("invalid_client"))
            return $"HTTP {(int)code}: Invalid Client ID or Client Secret.";
        if (body.Contains("invalid_grant") || body.Contains("LDAP"))
            return $"HTTP {(int)code}: Invalid username or password.";
        if (body.Contains("unauthorized_client"))
            return $"HTTP {(int)code}: Client not authorized for password grant. Check the OAuth app in ServiceNow.";
        if (code == System.Net.HttpStatusCode.NotFound)
            return "HTTP 404: Service URL not found. Verify the instance URL.";
        if (code == System.Net.HttpStatusCode.Unauthorized)
            return "HTTP 401: Unauthorized. Check credentials and OAuth configuration.";
        if (code == System.Net.HttpStatusCode.Forbidden)
            return "HTTP 403: Forbidden. The user may lack required roles in ServiceNow.";

        return $"HTTP {(int)code}: {body}";
    }
}