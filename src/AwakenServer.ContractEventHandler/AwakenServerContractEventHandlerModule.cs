using System;
using AElf.AElfNode.EventHandler.BackgroundJob.Options;
using AwakenServer.ContractEventHandler.Services;
using AwakenServer.Debits.Options;
using AwakenServer.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Threading;

namespace AwakenServer.ContractEventHandler
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AwakenServerEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AbpEventBusRabbitMqModule)
    )]
    public class AwakenServerContractEventHandlerModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostBuilderContext = context.Services.GetSingletonInstanceOrNull<HostBuilderContext>();
            
            var eventHandler = configuration.GetValue<string>("EventHandler");
            
            var newConfig = new ConfigurationBuilder().AddConfiguration(configuration)
                .AddJsonFile($"appsettings.{eventHandler}.json")
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .Build();
            
            hostBuilderContext.Configuration = newConfig;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            ConfigureCache(configuration);
            ConfigureRedis(context, configuration);
            context.Services.AddHostedService<AwakenHostedService>();
            Configure<AElfProcessorOption>(options =>
            {
                configuration.GetSection("AElfEventProcessors").Bind(options);
            });
            Configure<DebitOption>(configuration.GetSection("Debit"));
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var serviceProvider = context.ServiceProvider;
            var farmTokenInitializeService = serviceProvider.GetRequiredService<FarmTokenInitializeService>();
            AsyncHelper.RunSync(farmTokenInitializeService.InitializeFarmTokenAsync);
            
            var debitInitializeService = serviceProvider.GetRequiredService<DebitInitializeService>();
            AsyncHelper.RunSync(debitInitializeService.InitializeCompControllerAsync);
        }

        private void ConfigureCache(IConfiguration configuration)
        {
            Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = "AwakenServer:";
            });
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

            var redis = ConnectionMultiplexer.Connect(config);
            context.Services
                .AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "AwakenServer-Protection-Keys");
        }
    }
}