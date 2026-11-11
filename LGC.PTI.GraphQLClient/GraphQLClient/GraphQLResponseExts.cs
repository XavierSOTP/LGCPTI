using GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public static class GraphQLResponseExts
    {
        public static readonly Regex RegexTokenError =
            new Regex(pattern: $"^An unexpected error occured: Invalid authentication token.Error",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            matchTimeout: TimeSpan.FromMilliseconds(500));

        public static bool HasTokenError<T>(this GraphQLResponse<T> source)
        {
            var errors = source.Errors ?? Array.Empty<GraphQLError>();
            return errors
                 .Any(e => RegexTokenError.Match(e.Message).Success == true);
        }

        public static IEnumerable<GraphQLError> GetErrors<T>(this GraphQLResponse<T> source)
        {
            return source.Errors ?? Array.Empty<GraphQLError>();
        }
    }
}
