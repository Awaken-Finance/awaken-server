using AwakenServer.Debits.Entities.Ef;
using AwakenServer.Dividend.Entities.Ef;
using AwakenServer.Dividend.ETOs;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.ETOs.Debits;
using AwakenServer.ETOs.Farms;
using AwakenServer.ETOs.GameOfTrust;
using AwakenServer.Farms.Entities.Ef;
using AwakenServer.IDO.Entities.Ef;
using AwakenServer.IDO.ETOs;
using AwakenServer.MultiTenancy;
using AwakenServer.Price;
using AwakenServer.Price.Etos;
using AwakenServer.Trade;
using AwakenServer.Trade.Etos;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AuditLogging;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.IdentityServer;
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

                options.AutoEventSelectors.Add<UserLiquidity>();
                options.EtoMappings.Add<UserLiquidity, UserLiquidityEto>();

                options.AutoEventSelectors.Add<LiquidityRecord>();
                options.EtoMappings.Add<LiquidityRecord, LiquidityRecordEto>();

                options.AutoEventSelectors.Add<KLine>();
                options.EtoMappings.Add<KLine, KLineEto>();

                options.AutoEventSelectors.Add<LendingTokenPrice>();
                options.EtoMappings.Add<LendingTokenPrice, LendingTokenPriceEto>();
                
                options.AutoEventSelectors.Add<OtherLpToken>();
                options.EtoMappings.Add<OtherLpToken, OtherLpTokenEto>();

                AddFarmEntityEventOptions(options);
                AddDebitEntityEventOptions(options);
                AddGameOfTrustEntityEventOptions(options);
                AddIDOEntityEventOptions(options);
                AddDividendEntityEventOptions(options);
            });
        }

        private void AddFarmEntityEventOptions(AbpDistributedEntityEventOptions options)
        {
            options.AutoEventSelectors.Add<Farm>();
            options.EtoMappings.Add<Farm, FarmChangedEto>();
            options.AutoEventSelectors.Add<FarmPool>();
            options.EtoMappings.Add<FarmPool, FarmPoolChangedEto>();
            options.AutoEventSelectors.Add<FarmUserInfo>();
            options.EtoMappings.Add<FarmUserInfo, FarmUserInfoChangedEto>();
            options.AutoEventSelectors.Add<FarmRecord>();
            options.EtoMappings.Add<FarmRecord, FarmRecordChangedEto>();
        }
        
        private void AddDebitEntityEventOptions(AbpDistributedEntityEventOptions options)
        {
            options.AutoEventSelectors.Add<CompController>();
            options.EtoMappings.Add<CompController, CompControllerChangedEto>();
            options.AutoEventSelectors.Add<CToken>();
            options.EtoMappings.Add<CToken, CTokenChangedEto>();
            options.AutoEventSelectors.Add<CTokenUserInfo>();
            options.EtoMappings.Add<CTokenUserInfo, CTokenUserInfoChangedEto>();
            options.AutoEventSelectors.Add<CTokenRecord>();
            options.EtoMappings.Add<CTokenRecord, CTokenRecordChangedEto>();
        }
        
        private void AddGameOfTrustEntityEventOptions(AbpDistributedEntityEventOptions options)
        {
            options.AutoEventSelectors.Add<Entities.GameOfTrust.Ef.GameOfTrust>();
            options.EtoMappings.Add<Entities.GameOfTrust.Ef.GameOfTrust, GameChangedEto>();
            options.AutoEventSelectors.Add<GameOfTrustMarketData>();
            options.EtoMappings.Add<GameOfTrustMarketData, GameOfTrustMarketDataSnapshotEto>();
            options.AutoEventSelectors.Add<GameOfTrustRecord>();
            options.EtoMappings.Add<GameOfTrustRecord,GameOfTrustRecordCreatedEto>();
            options.AutoEventSelectors.Add<UserGameOfTrust>();
            options.EtoMappings.Add<UserGameOfTrust,UserGameOfTrustChangedEto>();
        }
        
        private void AddIDOEntityEventOptions(AbpDistributedEntityEventOptions options)
        {
            options.AutoEventSelectors.Add<PublicOffering>();
            options.EtoMappings.Add<PublicOffering, PublicOfferingEto>();
            options.AutoEventSelectors.Add<PublicOfferingRecord>();
            options.EtoMappings.Add<PublicOfferingRecord, PublicOfferingRecordEto>();
            options.AutoEventSelectors.Add<UserPublicOffering>();
            options.EtoMappings.Add<UserPublicOffering,UserPublicOfferingEto>();
        }

        private void AddDividendEntityEventOptions(AbpDistributedEntityEventOptions options)
        {
            options.AutoEventSelectors.Add<Dividend.Entities.Dividend>();
            options.EtoMappings.Add<Dividend.Entities.Dividend, DividendEto>();
            options.AutoEventSelectors.Add<DividendPool>();
            options.EtoMappings.Add<DividendPool, DividendPoolEto>();
            options.AutoEventSelectors.Add<DividendPoolToken>();
            options.EtoMappings.Add<DividendPoolToken, DividendPoolTokenEto>();
            options.AutoEventSelectors.Add<DividendToken>();
            options.EtoMappings.Add<DividendToken, DividendTokenEto>();
            options.AutoEventSelectors.Add<DividendUserRecord>();
            options.EtoMappings.Add<DividendUserRecord, DividendUserRecordEto>();
            options.AutoEventSelectors.Add<DividendUserToken>();
            options.EtoMappings.Add<DividendUserToken, DividendUserTokenEto>();
            options.AutoEventSelectors.Add<DividendUserPool>();
            options.EtoMappings.Add<DividendUserPool, DividendUserPoolEto>();
        }
    }
}
