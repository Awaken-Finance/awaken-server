using AwakenServer.Hubs;
using AwakenServer.Localization;
using AwakenServer.RabbitMq;
using Localization.Resources.AbpUi;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerApplicationContractsModule),
        typeof(AbpTenantManagementHttpApiModule),
        typeof(AbpSettingManagementHttpApiModule),
        typeof(AbpAspNetCoreSignalRModule)
    )]
    public class AwakenServerHttpApiModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            ConfigureLocalization();
            context.Services.AddMassTransit(x =>
            {
                x.AddConsumer<TradePairUpdatedHandler>();
                x.AddConsumer<NewTradeRecordHandler>();
                x.AddConsumer<NewKLineHandler>();
                x.AddConsumer<RemoveTradeRecordHandler>();
                x.AddConsumer<ActivityRankingListHandler>();
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var rabbitMqConfig = configuration.GetSection("MassTransit:RabbitMQ").Get<RabbitMqOptions>();
                    cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.Port, "/", h =>
                    {
                        h.Username(rabbitMqConfig.UserName);
                        h.Password(rabbitMqConfig.Password);
                    });
        
                    cfg.ReceiveEndpoint(rabbitMqConfig.ClientQueueName, e =>
                    {
                        e.ConfigureConsumer<TradePairUpdatedHandler>(ctx);
                        e.ConfigureConsumer<NewTradeRecordHandler>(ctx);
                        e.ConfigureConsumer<NewKLineHandler>(ctx);
                        e.ConfigureConsumer<RemoveTradeRecordHandler>(ctx);
                        e.ConfigureConsumer<ActivityRankingListHandler>(ctx);
                    });
                });
    
            });
        }

        private void ConfigureLocalization()
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Get<AwakenServerResource>()
                    .AddBaseTypes(
                        typeof(AbpUiResource)
                    );
            });
        }
    }
}
