using System;
using System.Net.Http;
using LGC.PIT.Plugin.Common.Configs;
using LGC.PTI.GraphQL.Transport;
using LGC.PTI.HTTPClient.Refit.Employees;
using Microsoft.Extensions.DependencyInjection;
using SoftSolute.MSDependencyInjection;

namespace LGC.PTI.HTTPClient.GraphQL;

/// <summary>Registers the PTI GraphQL client and its IEmployeeApiSrv adapter.</summary>
public sealed class GraphQLClientModule : MicrosoftDIServiceConfiguration
{
    public GraphQLClientModule()
    {
        AddService(services => services.AddHttpClient("PTI.GraphQL"));

        // One GraphQL client per plugin instance, so Firebase token caching is preserved.
        AddService(services => services.AddSingleton<IPtiGraphQLClient>(CreateClient));

        AddService(services => services.AddSingleton<IEmployeeApiSrv, GraphQLEmployeeApiSrv>());
    }

    private static IPtiGraphQLClient CreateClient(IServiceProvider provider)
    {
        var settings = provider.GetRequiredService<IConfigurableAppSettings>()
            .LGCSettings?.GraphQLSettings;

        if (settings == null ||
            !Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("PTI GraphQL URL or login credentials are not configured.");

        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("PTI.GraphQL");
        httpClient.BaseAddress = endpoint;

        return new PtiGraphQLClient(httpClient, settings.Email, settings.Password);
    }
}
