using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
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
            Log.Logger.Warning("==Orleans.IsRunningInKubernetes={0}", configSection.GetValue<bool>("IsRunningInKubernetes"));
            if (configSection.GetValue<bool>("IsRunningInKubernetes"))
            {
                Log.Logger.Warning("==Use kubernetes hosting...");
                UseKubernetesHostClustering(siloBuilder, configSection);
                Log.Logger.Warning("==Use kubernetes hosting end...");
            }
            else
            {
                Log.Logger.Warning("==Use docker hosting...");
                UseDockerHostClustering(siloBuilder, configSection);
                Log.Logger.Warning("==Use docker hosting end...");
            }
        });
    }

    private static void UseKubernetesHostClustering(ISiloBuilder siloBuilder, IConfigurationSection configSection)
    {
        Log.Logger.Warning("==Configuration");
        Log.Logger.Warning("==  POD_IP: {0}", Environment.GetEnvironmentVariable("POD_IP"));
        Log.Logger.Warning("==  SiloPort: {0}", configSection.GetValue<int>("SiloPort"));
        Log.Logger.Warning("==  GatewayPort: {0}", configSection.GetValue<int>("GatewayPort"));
        Log.Logger.Warning("==  DatabaseName: {0}", configSection.GetValue<string>("DataBase"));
        Log.Logger.Warning("==  ClusterId: {0}", Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID"));
        Log.Logger.Warning("==  ServiceId: {0}", Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID"));
        Log.Logger.Warning("==Configuration");
        siloBuilder /*.UseKubernetesHosting()*/
            .ConfigureEndpoints(advertisedIP: IPAddress.Parse(Environment.GetEnvironmentVariable("POD_IP") ?? string.Empty),
                siloPort: configSection.GetValue<int>("SiloPort"),
                gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
            .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = "GrainStorage";
                op.DatabaseName = configSection.GetValue<string>("DataBase");
                op.ConfigureJsonSerializerSettings = jsonSettings =>
                {
                    // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                    jsonSettings.NullValueHandling = NullValueHandling.Include;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                };
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
            .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
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
                .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configSection.GetValue<string>("DataBase");

                    op.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };
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
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(options =>
                {
                    options.Username = configSection.GetValue<string>("DashboardUserName");
                    options.Password = configSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = configSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
    }
}