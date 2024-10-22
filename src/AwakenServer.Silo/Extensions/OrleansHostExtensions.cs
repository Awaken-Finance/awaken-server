using System;
using System.Net;
using Awaken.Silo;
using Awaken.Silo.MongoDB;
using AwakenServer.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using Orleans.Statistics;
using Serilog;

namespace AwakenServer.Silo.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("Orleans");
        if (configSection == null)
            throw new ArgumentNullException(nameof(configSection), "The OrleansServer node is missing");
        return hostBuilder.UseOrleans((context,siloBuilder) =>
        {
            //Configure OrleansSnapshot
            configSection = context.Configuration.GetSection("Orleans");
            Log.Warning("==Orleans.IsRunningInKubernetes={0}", configSection.GetValue<bool>("IsRunningInKubernetes"));
            if (configSection.GetValue<bool>("IsRunningInKubernetes"))
            {
                Log.Warning("==Use kubernetes hosting...");
                UseKubernetesHostClustering(siloBuilder, configSection);
                Log.Warning("==Use kubernetes hosting end...");
            }
            else
            {
                Log.Warning("==Use docker hosting...");
                UseDockerHostClustering(siloBuilder, configSection);
                Log.Warning("==Use docker hosting end...");
            }
        });
    }

    private static void UseKubernetesHostClustering(ISiloBuilder siloBuilder, IConfigurationSection configSection)
    {
        Log.Warning("==Configuration");
        Log.Warning("==  POD_IP: {0}", Environment.GetEnvironmentVariable("POD_IP"));
        Log.Warning("==  SiloPort: {0}", configSection.GetValue<int>("SiloPort"));
        Log.Warning("==  GatewayPort: {0}", configSection.GetValue<int>("GatewayPort"));
        Log.Warning("==  DatabaseName: {0}", configSection.GetValue<string>("DataBase"));
        Log.Warning("==  ClusterId: {0}", Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID"));
        Log.Warning("==  ServiceId: {0}", Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID"));
        Log.Warning("==Configuration");
        siloBuilder /*.UseKubernetesHosting()*/
            .ConfigureEndpoints(
                advertisedIP: IPAddress.Parse(Environment.GetEnvironmentVariable("POD_IP") ?? string.Empty),
                siloPort: configSection.GetValue<int>("SiloPort"),
                gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
            .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .Configure<GrainCollectionNameOptions>(options =>
            {
                var collectionName = configSection
                    .GetSection(nameof(GrainCollectionNameOptions.GrainSpecificCollectionName)).GetChildren();
                options.GrainSpecificCollectionName = collectionName.ToDictionary(o => o.Key, o => o.Value);
            })
            .UseMongoDBReminders(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.CreateShardKeyForCosmos = false;
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID");
                options.ServiceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID");
            })
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); })
            .ConfigureServices(services =>
                services.AddSingleton<IGrainStateSerializer, AwakenJsonGrainStateSerializer>())
            .AddAwakenMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = OrleansConstants.GrainCollectionPrefix;
                op.DatabaseName = configSection.GetValue<string>("DataBase");

                var grainIdPrefix = configSection
                    .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                foreach (var kv in grainIdPrefix)
                {
                    Log.Information($"GrainSpecificIdPrefix, key: {kv.Key}, Value: {kv.Value}");
                }

                op.KeyGenerator = id =>
                {
                    var grainType = id.Type.ToString();
                    if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                    {
                        Log.Debug($"KeyGenerator, grainType: {grainType}, prefix: {prefix}");
                        return $"{prefix}+{id.Key}";
                    }

                    Log.Debug($"KeyGenerator, grainType: {grainType}, id: {id}");
                    return id.ToString();
                };
                op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
            });
        // .Services.AddSerializer(serializerBuilder =>
        // {
        //     serializerBuilder.AddCustomSerializer();
        // });

    }

    private static void UseDockerHostClustering(ISiloBuilder siloBuilder, IConfigurationSection configSection)
    {
        siloBuilder
                .ConfigureEndpoints(advertisedIP: IPAddress.Parse(configSection.GetValue<string>("AdvertisedIP")),
                    siloPort: configSection.GetValue<int>("SiloPort"),
                    gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .AddAwakenMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = OrleansConstants.GrainCollectionPrefix;
                op.DatabaseName = configSection.GetValue<string>("DataBase");

                var grainIdPrefix = configSection
                    .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                foreach (var kv in grainIdPrefix)
                {
                    Log.Information($"GrainSpecificIdPrefix, key: {kv.Key}, Value: {kv.Value}");
                }
                op.KeyGenerator = id =>
                {
                    var grainType = id.Type.ToString();
                    if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                    {
                        Log.Debug($"KeyGenerator, grainType: {grainType}, prefix: {prefix}");
                        return $"{prefix}+{id.Key}";
                    }
                    Log.Debug($"KeyGenerator, grainType: {grainType}, id: {id}");
                    return id.ToString();
                };
                op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
            })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                // .AddMemoryGrainStorage("PubSubStore")
                .UseDashboard(options =>
                {
                    options.Username = configSection.GetValue<string>("DashboardUserName");
                    options.Password = configSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = configSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                // .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        
    }
}