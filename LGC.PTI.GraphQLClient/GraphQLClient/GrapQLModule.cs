using LGC.PTI.Domain;
using LGC.PTI.Domain.GraphQLClient;
using Ninject;
using Ninject.Modules;
using System.Net.Http;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public class GraphQLModule : NinjectModule
    {
        public override void Load()
        {


            Bind<IHttpClientFactory>().ToMethod(ctx => ctx.Kernel.Get<GraphQLServiceProvider>().GetRequiredService<IHttpClientFactory>()).InSingletonScope();
            Bind<IGraphQLClientFactory>().To<GraphQLClientFactory>().InSingletonScope();
            Bind<IGraphQLClientSS>().To<GraphQLClientSS>();
#if (FAKEAPI)
            Bind<IPTIApiService>().To<FAKEPTIApiService>().InSingletonScope();

#else
            Bind<IPTIApiService>().To<PTIApiService>().InSingletonScope();
#endif
            Bind<ITokenHandler>().To<TokenHandler>().InSingletonScope();
            Bind<IAuthTokenStore>().To<AuthTokenStore>().InSingletonScope();
        }
    }
}
