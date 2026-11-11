using Microsoft.Extensions.DependencyInjection;
using SoftSolute.Ninject;
using System;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public class GraphQLServiceProvider : MicrosoftDIServiceProvider
    {
        public GraphQLServiceProvider()
        {
            base.AddService(s => s.AddHttpClient(name: GraphQLClientFactory.GRQL_HTTPCLIENT, client =>
           {
               client.Timeout = TimeSpan.FromSeconds(115);
           }).AddTransientHttpErrorPolicy(x => GraphQLPolly.GetRetryPolicy(x)));

            base.Build();
        }
    }


}
