using GraphQL;
using Polly.Retry;
using SoftSolute.Common.Utilities;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public interface ITokenHandler
    {
        SemaphoreSlim TokenRequestSemaphore { get; }
        Uri Url { get; }

        /// <summary>
        /// Policy to request a new access token. Once the token has been received it will update the context key "Token" and property "Token" of the IAuthTokenStore 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// 
        /// <returns></returns>
        AsyncRetryPolicy<GraphQLResponse<T>> CreateTokenRefreshPolicy<T>();
        /// <summary>
        /// Asynchronously Request a access token from API.
        /// </summary>
        /// <param name="caller"></param>
        /// <returns>A Task[Option[string]] with the most recent token if any, else it will request a token from the API. It returns <see cref="None{T}" /> on error</returns>
        Task<Option<T>> RequestAccessTokenAsync<T>([CallerMemberName] string caller = default);
        /// <summary>
        /// Get stored access token.
        /// </summary>
        /// <returns></returns>
        Task<string> GetTokenAsync();

        /// <summary>
        /// Asynchronously a wait any current token request complete
        /// </summary>
        /// <returns>A task that will complete when any ongoing token request is finished.</returns>
        Task WaitAsync();
    }
}