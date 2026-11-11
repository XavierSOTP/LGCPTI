using LGC.PIT.Plugin.Common.Configs;
using LGC.PTI.Domain.Common;
using LGC.PTI.HTTPClient.Polly;
using LGC.PTI.HTTPClient.Refit.Employees;
using Microsoft.Extensions.DependencyInjection;
using SoftSolute.MSDependencyInjection;

namespace LGC.PTI.HTTPClient.Refit;

/// <summary>
/// Configures dependency injection services required by the PTI HTTP client layer.
/// </summary>
/// <remarks>
/// Registers authentication, fault-handling policies, dynamic URL services,
/// HTTP message handlers, the Refit PTI employee client, and the employee API service.
/// </remarks>
public sealed class HtppClientModule : MicrosoftDIServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtppClientModule"/> class
    /// and registers the services required by the PTI HTTP client integration.
    /// </summary>
    public HtppClientModule()
    {
        AddService(s => s.AddSingleton<IAuthenticationSrv, AuthenticationSrv>());

        AddService(s => s.AddTransient<IFaultPolicies, FaultPolicies>());

        AddService(s => s.AddSingleton<IPTIDynamicUrlManager, PTIDynamicUrlManager>());

        AddService(s => s.AddSingleton<IPTISharableDataProvider, PTISharableDataProvider>());

        AddService(s => s.AddTransient<AuthHeaderHandler>());

        AddService(x => RegisterPtiApi(x));

        AddService(s => s.AddSingleton<IEmployeeApiSrv, EmployeeApiSrv>());
    }

    /// <summary>
    /// Registers and configures the Refit client used to communicate with the PTI Employee API.
    /// </summary>
    /// <param name="services">
    /// The service collection used to register the Refit client and its HTTP message handler.
    /// </param>
    /// <remarks>
    /// The client uses the application's default JSON serialization settings,
    /// reads the Employee API base address from <see cref="IConfigurableAppSettings"/>,
    /// and attaches <see cref="AuthHeaderHandler"/> to outgoing requests.
    /// </remarks>
    private static void RegisterPtiApi(IServiceCollection services)
    {
        services
            .AddRefitClient<IPtiEmployeeApi>(
                new RefitSettings
                {
                    ContentSerializer = new NewtonsoftJsonContentSerializer(
                        JsonSerializationHelper.DefaultJsonSerializerSettings),
                })
            .ConfigureHttpClient((sp, client) =>
            {
                var settings = sp.GetRequiredService<IConfigurableAppSettings>();

                client.BaseAddress = new Uri(settings.LGCSettings.ApiSettings.EmployeeUrl);
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();
    }
}