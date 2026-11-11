namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Defines the configuration values required to request an access token
/// from the PTI authentication endpoint.
/// </summary>
public sealed class TokenConfiguration
{
    /// <summary>
    /// Gets or sets the OAuth grant type used for authentication.
    /// </summary>
    /// <remarks>
    /// The default value is <c>client_credentials</c>.
    /// </remarks>
    [JsonProperty("grant_type")]
    public string GrantType { get; set; } = "client_credentials";

    /// <summary>
    /// Gets or sets the authentication scope requested for the access token.
    /// </summary>
    public string Scope { get; set; }
}