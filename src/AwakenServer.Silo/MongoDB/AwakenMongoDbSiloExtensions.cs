using Awaken.Silo.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Awaken.Silo.MongoDB;

public static class AwakenMongoDbSiloExtensions
{
    public static ISiloBuilder AddAwakenMongoDBGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<MongoDBGrainStorageOptions> configureOptions)
    {
        return builder.ConfigureServices((Action<IServiceCollection>) (services => services.AddAwakenMongoDBGrainStorage(name, configureOptions)));
    }

    public static IServiceCollection AddAwakenMongoDBGrainStorage(
        this IServiceCollection services,
        string name,
        Action<MongoDBGrainStorageOptions> configureOptions)
    {
        return services.AddAwakenMongoDBGrainStorage(name, (Action<OptionsBuilder<MongoDBGrainStorageOptions>>) (ob => ob.Configure(configureOptions)));
    }

    public static IServiceCollection AddAwakenMongoDBGrainStorage(
        this IServiceCollection services,
        string name,
        Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
    {
        if (configureOptions != null)
            configureOptions(services.AddOptions<MongoDBGrainStorageOptions>(name));
        services.TryAddSingleton<IGrainStorage>((Func<IServiceProvider, IGrainStorage>) (sp => sp.GetServiceByName<IGrainStorage>("Default")));
        services.TryAddSingleton<IGrainStateSerializer>((Func<IServiceProvider, IGrainStateSerializer>) (sp => (IGrainStateSerializer) new JsonGrainStateSerializer(sp, sp.GetService<IOptionsMonitor<MongoDBGrainStorageOptions>>().Get(name))));
        services.ConfigureNamedOptionForLogging<MongoDBGrainStorageOptions>(name);
        services.AddTransient<IConfigurationValidator>((Func<IServiceProvider, IConfigurationValidator>) (sp => (IConfigurationValidator) new MongoDBGrainStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>().Get(name), name)));
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        services.AddSingletonNamedService<IGrainStorage>(name, AwakenMongoGrainStorageFactory.Create);
        services.AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (Func<IServiceProvider, string, ILifecycleParticipant<ISiloLifecycle>>) ((s, n) => (ILifecycleParticipant<ISiloLifecycle>) s.GetRequiredServiceByName<IGrainStorage>(n)));
        return services;
    }
}
