using AwakenServer.MultiTenancy;
using AwakenServer.Trade;
using AwakenServer.Trade.Etos;
using AElf.Indexing.Elasticsearch;
using AwakenServer.StatInfo;
using AwakenServer.StatInfo.Etos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AuditLogging;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.Emailing;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerDomainSharedModule),
        typeof(AbpAuditLoggingDomainModule),
        typeof(AbpSettingManagementDomainModule),
        typeof(AbpTenantManagementDomainModule),
        typeof(AbpEmailingModule),
        typeof(AElfIndexingElasticsearchModule)
    )]
    public class AwakenServerDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpMultiTenancyOptions>(options => { options.IsEnabled = MultiTenancyConsts.IsEnabled; });

#if DEBUG
            context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AwakenServerDomainModule>(); });

            Configure<AbpDistributedEntityEventOptions>(options =>
            {
                options.AutoEventSelectors.Add<TradePair>();
                options.EtoMappings.Add<TradePair, TradePairEto>();
                
                options.AutoEventSelectors.Add<TradePairMarketDataSnapshot>();
                options.EtoMappings.Add<TradePairMarketDataSnapshot, TradePairMarketDataSnapshotEto>();
                
                options.AutoEventSelectors.Add<TradeRecord>();
                options.EtoMappings.Add<TradeRecord, TradeRecordEto>();
                options.EtoMappings.Add<TradeRecord, MultiTradeRecordEto>();

                options.AutoEventSelectors.Add<UserLiquidity>();
                options.EtoMappings.Add<UserLiquidity, UserLiquidityEto>();

                options.AutoEventSelectors.Add<LiquidityRecord>();
                options.EtoMappings.Add<LiquidityRecord, LiquidityRecordEto>();

                options.AutoEventSelectors.Add<KLine>();
                options.EtoMappings.Add<KLine, KLineEto>();
                
                options.AutoEventSelectors.Add<StatInfoSnapshot>();
                options.EtoMappings.Add<StatInfoSnapshot, StatInfoSnapshotEto>();
            });
        }
    }
}
