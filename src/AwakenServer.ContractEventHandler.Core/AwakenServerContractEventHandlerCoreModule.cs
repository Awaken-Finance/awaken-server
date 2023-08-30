using AElf.AElfNode.EventHandler.BackgroundJob;
using AwakenServer.ContractEventHandler.Price.Processors;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.ContractEventHandler.Trade;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Processors;
using AwakenServer.Debits.Options;
using AwakenServer.Farms.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AwakenServer.ContractEventHandler
{
    [DependsOn(
        typeof(AwakenServerApplicationModule),
        // typeof(AElfEthereumEventHandlerCoreModule),
        typeof(AElfEventHandlerBackgroundJobModule)
    )]
    public class AwakenServerContractEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            Configure<ApiOptions>(configuration.GetSection("Api"));

            //Price
            Configure<ChainlinkAggregatorOptions>(configuration.GetSection("ChainlinkAggregator"));
            context.Services.AddTransient<AnswerUpdatedEventProcessor>();
            context.Services.AddTransient<PriceUpdatedEventProcessor>();
            context.Services.AddTransient<OtherLpSyncEventProcessor>();

            //Trade
            Configure<FactoryContractOptions>(configuration.GetSection("FactoryContract"));
            context.Services.AddTransient<SyncEventProcessor>();
            context.Services.AddTransient<SwapEventProcessor>();
            context.Services.AddTransient<BurnEventProcessor>();
            context.Services.AddTransient<MintEventProcessor>();
            Configure<TradePairTokenOrderOptions>(configuration.GetSection("TradePairTokenOrder"));

            context.Services.AddSingleton(typeof(ICachedDataProvider<>), typeof(DefaultCacheDataProvider<>));

            Configure<AbpAutoMapperOptions>(options =>
            {
                //Add all mappings defined in the assembly of the MyModule class
                options.AddMaps<AwakenServerContractEventHandlerCoreModule>();
            });

            //GameOfTrust
            Configure<AnchorCoinsOptions>(options => { configuration.GetSection("AnchorCoins").Bind(options); });

            //Farm
            Configure<FarmOption>(p => { configuration.GetSection("Farm").Bind(p); });

            //Debit
            Configure<DebitOption>(p => { configuration.GetSection("Debit").Bind(p); });
        }
    }
}