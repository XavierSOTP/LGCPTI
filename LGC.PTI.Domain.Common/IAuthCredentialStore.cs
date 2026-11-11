using LanguageExt;
using SoftSolute.Common.Utilities;

namespace LGC.PTI.Domain.Common
{
    /// <summary>
    /// Manages storage and updates of authentication credentials (token URL, client ID, and client secret).
    /// Provides token retrieval and change notifications for credential updates.
    /// </summary>
    public interface IAuthCredentialStore
    {
        /// <summary>
        /// Gets the OAuth token URL used for authentication.
        /// </summary>
        string TokenUrl { get; }

        /// <summary>
        /// Gets the client ID used for authentication.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Gets the client secret used for authentication.
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Updates the stored authentication credentials.
        /// </summary>
        /// <param name="tokenUrl">The new OAuth token URL.</param>
        /// <param name="clientId">The new client ID.</param>
        /// <param name="clientSecret">The new client secret.</param>
        void UpdateCredentials(
            string tokenUrl,
            string clientId,
            string clientSecret);

        /// <summary>
        /// Registers an event handler to be notified when credentials change.
        /// </summary>
        /// <param name="listenerId">Unique identifier for the listener.</param>
        /// <param name="eventHandler">Async handler to execute when credentials change.</param>
        /// <returns>An effect wrapping the registration result.</returns>
        Eff<Unit> RegisterToCredentialsChanged(
            NotEmptyString listenerId,
            Func<Task<Unit>> eventHandler);

        /// <summary>
        /// Clears the cached authentication token.
        /// </summary>
        /// <returns>A unit value indicating successful reset.</returns>
        Unit ResetToken();

        /// <summary>
        /// Attempts to retrieve the cached authentication token.
        /// </summary>
        /// <returns>An Option containing the token if available, or None if not cached.</returns>
        Option<string> TryGetToken();

        /// <summary>
        /// Updates the cached authentication token.
        /// </summary>
        /// <param name="token">The new authentication token.</param>
        /// <returns>A unit value indicating successful update.</returns>
        Unit UpdateToken(
            NotEmptyString token);
    }
}