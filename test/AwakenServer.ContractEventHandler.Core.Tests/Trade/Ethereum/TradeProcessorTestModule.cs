using System.Collections.Generic;
using AElf.EthereumNode.EventHandler.BackgroundJob.Options;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Trade;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Processors;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AwakenServer.Trade.Ethereum
{
    [DependsOn(
        typeof(AwakenServerContractEventHandlerCoreTestModule),
        typeof(AElfEthereumEventHandlerTestBaseModule),
        typeof(TradeTestModule)
    )]
    public class TradeProcessorTestModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(o => Mock.Of<IPairEventSubscribeProvider>());

            context.Services.Configure<FactoryContractOptions>(options =>
            {
                options.Contracts = new Dictionary<string, double>
                {
                    {"0xFactoryA", 0.0003},
                    {"0xFactoryB", 0.0005},
                };
            });
            
            Configure<EthereumProcessorOption>(p =>
            {
                p.IsEnableRepository = false;
                p.IsCheckRepeatEvent = false;
            });
        }
    }
}