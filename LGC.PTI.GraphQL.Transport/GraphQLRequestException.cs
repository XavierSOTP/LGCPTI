using System;
using System.Net;

namespace LGC.PTI.GraphQL.Transport
{
    public sealed class GraphQLRequestException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public GraphQLRequestException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public GraphQLRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
