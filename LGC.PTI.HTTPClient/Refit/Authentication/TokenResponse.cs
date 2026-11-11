namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Represents the access token response returned by the PTI authentication endpoint.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// Gets or sets the access token issued by the authentication server.
    /// </summary>
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds before the access token expires.
    /// </summary>
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the type of token returned by the authentication server.
    /// </summary>
    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    /// <summary>
    /// Gets or sets the scope associated with the issued access token.
    /// </summary>
    public string Scope { get; set; }
}