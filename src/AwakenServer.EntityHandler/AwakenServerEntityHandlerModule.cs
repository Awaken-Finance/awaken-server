using System;
using AElf.Indexing.Elasticsearch.Options;
using AwakenServer.Chains;
using AwakenServer.CoinGeckoApi;
using AwakenServer.AetherLinkApi;
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
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
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
using JsonConvert = Newtonsoft.Json.JsonConvert;
using LogLevel = Com.Ctrip.Framework.Apollo.Logging.LogLevel;

namespace AwakenServer.EntityHandler;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AwakenServerEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AwakenServerEntityHandlerCoreModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AwakenServerWorkerModule),
    typeof(AwakenServerAetherLinkApiModule)
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

        Configure<ChainsInitOptions>(configuration.GetSection("ChainsInit"));
        Configure<ApiOptions>(configuration.GetSection("Api"));
        Configure<WorkerOptions>(configuration.GetSection("WorkerSettings"));
        Configure<TradeRecordRevertWorkerSettings>(configuration.GetSection("WorkerSettings:Workers:TransactionRevert"));
        Configure<DataCleanupWorkerSettings>(configuration.GetSection("WorkerSettings:Workers:DataCleanup"));
        Configure<StatInfoUpdateWorkerSettings>(configuration.GetSection("WorkerSettings:Workers:StatInfoUpdateEvent"));
        
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
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
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
}