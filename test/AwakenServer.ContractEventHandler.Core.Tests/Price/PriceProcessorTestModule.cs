using System.Collections.Generic;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Price.Processors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;

namespace AwakenServer.Price
{
    [DependsOn(typeof(AwakenServerContractEventHandlerCoreTestModule),
        typeof(AElfEthereumEventHandlerTestBaseModule))]
    public class PriceProcessorTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);

            Configure<ChainlinkAggregatorOptions>(options =>
            {
                options.Aggregators = new Dictionary<string, ChainlinkAggregator>
                {
                    {
                        "Ethereum-0xAggregator", new ChainlinkAggregator
                        {
                            Token = "0xBTC",
                            Decimals = 18
                        }
                    }
                };
            });

            context.Services.RemoveAll<IBlockchainClientProvider>();
            context.Services.AddTransient<IBlockchainClientProvider, MockWeb3Provider>();
        }
    }
}