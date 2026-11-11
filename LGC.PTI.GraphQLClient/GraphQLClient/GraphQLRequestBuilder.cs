using GraphQL;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.CollectionSS;
using SoftSolute.Common.Utilities.ReflectionSS;
using SoftSolute.Common.Utilities.StringSS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public static class GraphQLRequestBuilder
    {
        private const string close = "}";

        private const string open = "{";

        private const string openQuery = @"";

        public static GraphQLRequest CreateQuery<TProjection>(string methodSyntax, string querySyntax = default, dynamic variables = null)
        {
            IEnumerable<string> properties = Array.Empty<string>();
            var foundProps = _propertyMapping.TryGetValue(typeof(TProjection));
            foundProps.IfNone(() =>
            {
                properties = GetProperties<TProjection>();
                _propertyMapping.AddOrUpdate(typeof(TProjection), properties);
            });
            foundProps.IfSome(p => properties = p);

            var sb = new StringBuilder(openQuery);
            querySyntax = querySyntax.IsNullOrEmpty() ? RootType.query.ToString() : querySyntax;
            var propsStr = properties.Select(s => s.ToCamelCase()).ConvertToString(",");
            var query = sb
                .Append(querySyntax)
                .Append(open)
                .Append(methodSyntax)
                .Append(open)
                .Append(propsStr)
                .Append(close)
                .Append(close);

            var request = new GraphQLRequest
            {
                Query = query.ToString()
            };

            if (variables is object)
            {
                request.Variables = variables;
            }
            return request;
        }

        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> _propertyMapping = new ConcurrentDictionary<Type, IEnumerable<string>>();

        private enum RootType
        {
            query = 2,
            mutation = 4,
            subscription = 8
        }

        private static IEnumerable<string> GetProperties<T>()
        {
            return ReflectionHelper.GetProperties<T>().Select(p => p.Name);
        }
    }
}