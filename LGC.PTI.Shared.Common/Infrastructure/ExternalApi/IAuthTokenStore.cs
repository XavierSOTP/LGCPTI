using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LGC.PTI.Domain
{
    public interface IAuthTokenStore
    {
        string Password { get; }
        /// <summary>
        /// Get stored token
        /// </summary>
        /// <returns> </returns>
        Task<string> GetTokenAsync();
        Uri Url { get; set; }
        string User { get; set; }
        /// <summary>
        /// DateTime of the latest update of the token
        /// </summary>
        DateTime LastUpdate { get; set; }

        /// <summary>
        /// Request to block token, that it cannot be modified by other threads.
        /// </summary>
        /// <returns> </returns>
        Task StartEditTokenAsync([CallerMemberName] string caller = default);

        /// <summary>
        /// Unblock token if it is blocked, that it can be modified by other threads.
        /// </summary>
        /// <returns> </returns>
        void TryEndEditToken([CallerMemberName] string caller = default);

        ///// <summary>
        /////
        ///// </summary>
        //void EnableTokenUpdate(bool value);

        /// <summary>
        /// Acknowledge reception of all related data to request a token from an API
        /// </summary>
        event EventHandler ApiInfoComplete;
        /// <summary>
        /// </summary>
        event EventHandler TokenUpdated;

        /// <summary>
        /// </summary>
        /// <param name="token"> </param>
        /// <returns> </returns>
        Task UpdateTokenAsync(string token);

        /// <summary>
        /// Validate if token has being recently updated.
        /// </summary>
        /// <returns> True is token has been updated less or equal to 10 seconds </returns>
        bool IsRecentlyUpdated();
        /// <summary>
        /// </summary>
        /// <param name="value"> </param>
        /// <param name="notify">
        /// If notify is true, it will trigger the delay in the notification mechanism, which will
        /// wait for all related data to be received within a time frame.s
        /// </param>
        void UpdatePassword(string value, bool notify = true);
    }
}