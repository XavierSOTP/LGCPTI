namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Defines the PTI authentication API used to request access tokens.
/// </summary>
public interface IPTIAuthClientApi
{
    /// <summary>
    /// Sends an authentication request to the PTI token endpoint.
    /// </summary>
    /// <param name="authorizationheader">
    /// The HTTP Authorization header containing the encoded client credentials.
    /// </param>
    /// <param name="body">
    /// The token request configuration serialized as URL-encoded form data.
    /// </param>
    /// <returns>
    /// A task that returns the token response received from the PTI authentication endpoint.
    /// </returns>
    [Post("/token")]
    Task<TokenResponse> SendTokenRequest(
        [Header("Authorization")] string authorizationheader,
        [Body(BodySerializationMethod.UrlEncoded)] TokenConfiguration body);
}