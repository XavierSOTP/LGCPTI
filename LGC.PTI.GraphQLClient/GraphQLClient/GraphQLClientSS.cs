using GraphQL;
using GraphQL.Client.Http;
using Nito.AsyncEx.Synchronous;
using NLog;
using SoftSolute.Common.Utilities;
using SoftSolute.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public interface IGraphQLClientSS
    {
        Task<Option<GraphQLResponse<T>>> SendQueryAsync<T>(GraphQLRequest request);
    }


    public class GraphQLClientSS : IGraphQLClientSS
    {
        private readonly ILogger _logger;
        private readonly IGraphQLClientFactory _graphQLClientFactory;
        private readonly ITokenHandler _tokenHandler;

        public GraphQLClientSS(IGraphQLClientFactory graphQLClientFactory, ITokenHandler tokenHandler, ILogger logger)
        {

            _logger = logger.Factory.GetCurrentClassLogger();
            _graphQLClientFactory = graphQLClientFactory;
            _tokenHandler = tokenHandler;
        }


        public async Task<Option<GraphQLResponse<T>>> SendQueryAsync<T>(GraphQLRequest request)
        {
            try
            {
                _logger.LogEnteringMethod();
                var policy = _tokenHandler.CreateTokenRefreshPolicy<T>();
                var response = await policy.ExecuteAsync(async (context, ct) =>
                {
                    await _tokenHandler.WaitAsync();
                    var token = await _tokenHandler.GetTokenAsync();
                    var graphQLClient = _graphQLClientFactory.CreateAuthenticatedClient(_tokenHandler.Url, token.ToOption());

                    return await graphQLClient.SendQueryAsync<T>(request);
                }, TokenContext.CreateContext(_tokenHandler.GetTokenAsync().WaitAndUnwrapException()), cancellationToken: default);
                if (response.GetErrors().Count().IsGt(0))
                {
                    foreach (var err in response.GetErrors())
                    {
                        _logger.Error($"{err.Path},{err.Message}");
                    }
                    _logger.LogLeavingMethod();
                    return None.Value;
                }
                _logger.LogLeavingMethod();
                return response;
            }
            catch (GraphQLHttpRequestException ex)
            {
                _logger.Error(ex.Content);
                return None.Value;
            }
            catch (Exception ex)
            {
                var type = ex.GetType();
                _logger.Error(ex);
                throw;
            }
        }
    }
}
