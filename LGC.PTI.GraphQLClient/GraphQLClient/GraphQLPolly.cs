using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Net.Http;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public static class GraphQLPolly
    {
        public const int RETRY = 3;
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PolicyBuilder<HttpResponseMessage> policyBuilder)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(
                 medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: RETRY);

            var retryPolicy = policyBuilder
               .WaitAndRetryAsync
                (delay, (exception, timespan) =>
                {
                });

            return retryPolicy;
        }

    }
}