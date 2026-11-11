using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using SoftSolute.Common.Utilities;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LGC.PTI.Infrastructure
{
    public interface IGraphQLClientFactory
    {
        GraphQLHttpClient CreateAuthenticatedClient(Uri url, Option<string> token);
        GraphQLHttpClient CreateClient(Uri url);
    }

    public class GraphQLClientFactory : IGraphQLClientFactory
    {
        internal const string GRQL_HTTPCLIENT = "GrQlHttpClient";
        private readonly IHttpClientFactory _httpClientFactory;

        public GraphQLClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        public GraphQLHttpClient CreateAuthenticatedClient(Uri url, Option<string> token)
        {
            return InternalCreateClient(_httpClientFactory, url, token);
        }

        public GraphQLHttpClient CreateClient(Uri url)
        {
            return InternalCreateClient(_httpClientFactory, url);
        }

        private static GraphQLHttpClient InternalCreateClient(IHttpClientFactory httpClientFactory, Uri url, Option<string> token = default)
        {
            var httpClient = httpClientFactory.CreateClient(GRQL_HTTPCLIENT);
            token = token ?? None.Value;
            token.IfSome(t => httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(t));

            var graphQLClient = new GraphQLHttpClient(
                options: new GraphQLHttpClientOptions { EndPoint = url },
                serializer: new NewtonsoftJsonSerializer(),
                httpClient: httpClient);
            return graphQLClient;
        }
    }
}