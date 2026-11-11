using GraphQL;
using LGC.PTI.Domain;
using NLog;
using Polly;
using Polly.Retry;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.CollectionSS;
using SoftSolute.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure.GraphQLClient
{


    public class TokenHandler : ITokenHandler
    {

        private readonly ILogger _logger;
        private readonly IGraphQLClientFactory _graphQLClientFactory;
        private readonly IAuthTokenStore _authTokenStore;

        public TokenHandler(ILogger logger, IGraphQLClientFactory graphQLClientFactory, IAuthTokenStore authTokenStore)
        {
            _logger = logger;
            _graphQLClientFactory = graphQLClientFactory;
            _authTokenStore = authTokenStore;
            TokenRequestSemaphore = new SemaphoreSlim(1);
            _authTokenStore.ApiInfoComplete += TokenApiInfoComplete;
        }


        #region Public Props
        public SemaphoreSlim TokenRequestSemaphore { get; private set; }

        public Uri Url => _authTokenStore.Url;

        public Task<string> GetTokenAsync() => _authTokenStore.GetTokenAsync();

        #endregion

        #region Public Methods


        /// <summary>
        /// Policy to request a new access token. Once the token has been received it will update the context key "Token" and property "Token" of the IAuthTokenStore 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// 
        /// <returns></returns>
        public AsyncRetryPolicy<GraphQLResponse<T>> CreateTokenRefreshPolicy<T>()
        {
            var policy = Policy
                .HandleResult<GraphQLResponse<T>>(message => message.HasTokenError())
                .RetryAsync(1, async (result, retryCount, context) =>
                {
                    await RetryGetAccessTokenAsync(context);
                });

            return policy;
        }




        /// <summary>
        /// Asynchronously Request a access token from API.
        /// </summary>
        /// <param name="caller"></param>
        /// <returns>A Task[Option[string]] with the most recent token if any, else it will request a token from the API. It returns <see cref="None{T}" /> on error</returns>
        public async Task<Option<T>> RequestAccessTokenAsync<T>([CallerMemberName] string caller = default)
        {
            try
            {
                if (_authTokenStore.IsRecentlyUpdated())
                {
                    await _authTokenStore.GetTokenAsync();
                }
                _logger.Info($"{caller} has requested a new token");
                var loginRequest = CreateLoginQuery();
                var graphQLClient = _graphQLClientFactory.CreateClient(_authTokenStore.Url);
                var graphQLResponse = await graphQLClient.SendMutationAsync<T>(loginRequest);
                if (graphQLResponse.Errors?.Any() ?? false)
                {
                    graphQLResponse.Errors.ForEach(err => _logger.Error($"{err.Path},{err.Message}"));
                    return None.Value;
                }
                _logger.Info("token received");
                return graphQLResponse.Data.ToOption();

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }

        }


        /// <summary>
        /// Asynchronously a wait any current token request complete
        /// </summary>
        /// <returns>A task that will complete when any ongoing token request is finished.</returns>
        public async Task WaitAsync()
        {
            while (TokenRequestSemaphore.CurrentCount == 0)
            {
                await Task.Delay(50);
            }
        }
        #endregion

        #region Private Methods

        private async void TokenApiInfoComplete(object sender, EventArgs e)
        {
            _logger.LogEnteringMethod();
            try
            {
                await _authTokenStore.StartEditTokenAsync();
                var newTokenResponse = await RequestAccessTokenAsync<TokenReponse>();
                await newTokenResponse
                    .Map(resp => resp.Login.Token.ToOption())
                    .IfSomeAsync(t => _authTokenStore.UpdateTokenAsync(t));
                _logger.LogLeavingMethod();

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
            finally
            {
                _authTokenStore.TryEndEditToken();
            }
        }


        private GraphQLRequest CreateLoginQuery()
        {
            var query = @"mutation($email:String!, $password:String!){
                            login(email:$email,password:$password){
                                token
                                }
                            }";
            var variables = new { email = _authTokenStore.User, password = _authTokenStore.Password };
            var loginRequest = new GraphQLRequest
            {
                Query = query,
                Variables = variables
            };
            return loginRequest;
        }
        private async Task RetryGetAccessTokenAsync(Context context)
        {
            if (CanRequestToken())
            {

                var aquireSemaphoreAsync = TokenRequestSemaphore.WaitAsync();
                try
                {
                    await aquireSemaphoreAsync;

                    //For the first call when the token is null, the condition will be true an a request for new token will be created. the Token property will be updated and the semaphore released.
                    // Subsequence requests have been waiting will skip this step as token is not longer null.

                    if (_authTokenStore.IsRecentlyUpdated())
                    {
                        return;
                    }
                    await _authTokenStore.StartEditTokenAsync();
                    var newAccessToken = await RequestAccessTokenAsync<TokenReponse>();
                    var token = newAccessToken
                        .Map(resp => resp.Login.Token.ToOption())
                        .IfSome(t => context.SetToken(t));
                    token.IfSome(t => _authTokenStore.UpdateTokenAsync(t));


                }
                catch (Exception)
                {
                    _logger.Error("retrieving token failed.");
                    throw;
                }
                finally
                {
                    if (aquireSemaphoreAsync?.Status == TaskStatus.RanToCompletion)
                    {
                        TokenRequestSemaphore.Release();
                    }
                    _authTokenStore.TryEndEditToken();

                }
                return;
            }
            while (!CanRequestToken())
            {

                await Task.Delay(50);
            }


        }

        private bool CanRequestToken()
        {
            return TokenRequestSemaphore.CurrentCount == 1;
        }

        #endregion
    }
}