using System;
using AElf.Indexing.Elasticsearch.Options;
using AwakenServer.Chains;
using AwakenServer.CoinGeckoApi;
using AwakenServer.EntityFrameworkCore;
using AwakenServer.Grains;
using AwakenServer.RabbitMq;
using AwakenServer.Trade;
using AwakenServer.Worker;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AwakenServer.EntityHandler;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AwakenServerEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AwakenServerEntityHandlerCoreModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AwakenServerWorkerModule),
    typeof(AwakenServerCoinGeckoApiModule)
)]
public class AwakenServerEntityHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureCache(configuration);
        ConfigureRedis(context, configuration);
        ConfigureAuditing();
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<AwakenHostedService>();
        ConfigureOrleans(context, configuration);

        Configure<ChainsInitOptions>(configuration.GetSection("ChainsInit"));

        Configure<ApiOptions>(configuration.GetSection("Api"));

        Configure<TradeRecordOptions>(configuration.GetSection("TradeRecord"));
        
        Configure<TradePairEventSyncOptions>(configuration.GetSection("TradePairEventSync"));
        
        Configure<SwapEventSyncOptions>(configuration.GetSection("SwapEventSync"));

        context.Services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rabbitMqConfig = configuration.GetSection("MassTransit:RabbitMQ").Get<RabbitMqOptions>();
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.Port, "/", h =>
                {
                    h.Username(rabbitMqConfig.UserName);
                    h.Password(rabbitMqConfig.Password);
                });
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        StopOrleans(context.ServiceProvider);
    }

    private void ConfigureAuditing()
    {
        Configure<AbpAuditingOptions>(options => { options.IsEnabled = false; });
    }

    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(AwakenServerDomainModule)); });
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AwakenServer:"; });
    }

    private void ConfigureRedis(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var config = configuration["Redis:Configuration"];
        if (string.IsNullOrEmpty(config))
        {
            return;
        }

        var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
        context.Services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "AwakenServer-Protection-Keys");
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(AwakenServerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }

    private static void StartOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    private static void StopOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}