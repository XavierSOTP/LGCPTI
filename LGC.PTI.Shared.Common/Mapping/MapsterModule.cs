using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using SoftSolute.MSDependencyInjection;

namespace LGC.PTI.Domain.Infrastructure.Mapping;

/// <summary>
/// Configures and registers Mapster mapping services for the PTI domain layer.
/// </summary>
public sealed class MapsterModule : MicrosoftDIServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapsterModule"/> class
    /// and registers the Mapster configuration with the dependency injection container.
    /// </summary>
    public MapsterModule()
    {
        AddService(x => RegisterMapster(x));
    }

    /// <summary>
    /// Registers the Mapster configuration and mapper implementation
    /// in the specified service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection used to register the Mapster dependencies.
    /// </param>
    private static void RegisterMapster(IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.Scan(typeof(MapsterModule).Assembly);

        services.AddSingleton(config);
        services.AddTransient<IMapper, Mapper>();
    }
}