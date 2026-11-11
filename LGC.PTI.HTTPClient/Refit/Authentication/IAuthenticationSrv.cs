using LGC.PTI.Domain.Common;

namespace LGC.PTI.Refit.Authentication
{
    /// <summary>
    /// Provides authentication services for PTI API communication.
    /// Manages token retrieval and credential changes for OAuth authentication.
    /// </summary>
    public interface IAuthenticationSrv
    {
        /// <summary>
        /// Gets the underlying authentication credential store managing credentials and tokens.
        /// </summary>
        IAuthCredentialStore AuthTokenStore { get; }

        /// <summary>
        /// Notifies the service that stored credentials have changed.
        /// Useful for updating cached tokens or re-authenticating after credential updates.
        /// </summary>
        /// <returns>A task that returns a unit value when notification is complete.</returns>
        Task<Unit> CredentialsChangedAsync();

        #region Public Methods

        /// <summary>
        /// Attempts to retrieve an authentication token asynchronously.
        /// </summary>
        /// <returns>A task that returns an Option containing the token if successful, or None if unavailable.</returns>
        Task<Option<string>> TryGetTokenAsync();

        #endregion Public Methods
    }
}