using LGC.PIT.Plugin.Common;
using LGC.PTI.Domain;
using LGC.PTI.Domain.Common;
using LGC.PTI.Domain.Observers;
using LGC.PTI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SoftSolute.MSDependencyInjection;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Configures and registers all application services with the Microsoft Dependency Injection container.
/// This sealed class manages the registration of singleton instances for observers, license management,
/// credential storage, and employee management services required by the PTI plugin.
/// </summary>
public sealed class AppServiceConfiguration : MicrosoftDIServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppServiceConfiguration"/> class and registers all required services.
    /// Registers observers (CardholderObserver, CredentialObserver) and their interfaces,
    /// license management services, authentication stores, and employee management services.
    /// </summary>
    public AppServiceConfiguration()
    {
        AddService(s => s.AddSingleton<IAppInitializedNotifierPlugin, AppInitializedNotifierPlugin>());
        AddService(services =>
            services.AddSingleton<SdkApplication>());
        AddService(x =>
    x.AddSingleton(new JsonSerializerSettings()));
        AddService(services =>
           services.AddSingleton<
      SoftSolute.Licensing.ILicenseExpirator,
      SoftSolute.Licensing.LicenseExpirator>());
        AddService(services =>
            services.AddSingleton<IUnInitLicenseManager>(serviceProvider =>
                PluginLicenseManager.Create(
                    serviceProvider.GetRequiredService<ILogger<PluginLicenseManager>>(),
                    serviceProvider.GetRequiredService<SoftSolute.Licensing.ILicenseExpirator>())));
        AddService(services =>
      services.AddSingleton<CardholderObserver>());

        AddService(services =>
            services.AddSingleton<ICardholderObserver>(sp =>
                sp.GetRequiredService<CardholderObserver>()));
        AddService(services =>
            services.AddSingleton<CredentialObserver>());
        AddService(services =>
            services.AddSingleton<IEmployeeManager, EmployeeManager>());

        AddService(services =>
            services.AddSingleton<IAuthCredentialStore, AuthCredentialStore>());

        AddService(services =>
            services.AddSingleton<ICredentialStateHandler, CredentialStateHandler>());
        AddService(services =>
            services.AddSingleton<IPipeAppClient, PipeAppClient>());
    }
}
