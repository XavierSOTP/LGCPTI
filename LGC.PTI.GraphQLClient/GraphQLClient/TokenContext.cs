using Polly;
using SoftSolute.Common.Utilities;
using System.Collections.Generic;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public static class TokenContext
    {
        private const string accesTokenStr = "Access_Token";

        public static void SetToken(this Context context, string token)
            => context[accesTokenStr] = token;

        public static Option<string> GetToke(this Context context)
            => context[accesTokenStr]?.ToString().ToOption();

        public static Context CreateContext(string token)
            => new Context(null, new Dictionary<string, object>() {
                { accesTokenStr , token }
            });
    }
}