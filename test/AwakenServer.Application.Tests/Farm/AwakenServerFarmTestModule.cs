using System.Collections.Generic;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler;
using AwakenServer.Farms.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AwakenServer.Farm
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AElfEthereumEventHandlerTestBaseModule),
        typeof(AElfEventHandlerTestBaseModule)
    )]
    public class AwakenServerFarmTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            MockFarmPriceAppService(services);
            Configure<FarmTokenOptions>(options =>
            {
                options.FarmTokens = new Dictionary<string, FarmToken>();
                AddFarmSwapTokenOneOptions(options);
                AddFarmSwapTokenTwoOptions(options);
                AddFarmSwapTokenThreeOptions(options);
                AddFarmSwapTokenFourOptions(options);
            });
        }

        private void MockFarmPriceAppService(IServiceCollection services)
        {
            services.RemoveAll<IFarmAppPriceService>();
            services.AddTransient(typeof(IFarmAppPriceService), typeof(MockFarmAppPriceService));
            services.RemoveAll<IBlockchainClientProvider>();
            services.AddSingleton<IBlockchainClientProvider, MockAElfClientProvider>();
        }

        private void AddFarmSwapTokenOneOptions(FarmTokenOptions p)
        {
            var farmTokenOne =  new FarmToken
            {
                ChainName = FarmTestData.DefaultNodeName,
                Symbol = FarmTestData.SwapTokenOneSymbol,
                Decimals = FarmTestData.SwapTokenOneDecimal,
                Address = FarmTestData.SwapTokenOneContractAddress,
                Type = FarmTokenType.LpToken,
                LendingPool = string.Empty,
                Tokens = new[]
                {
                    new TokenOption
                    {
                        Symbol = FarmTestData.SwapTokenOneToken1Symbol,
                        Decimals = FarmTestData.SwapTokenOneToken1Decimal,
                        Address = FarmTestData.SwapTokenOneToken1ContractAddress,
                    },
                    new TokenOption
                    {
                        Symbol = FarmTestData.SwapTokenOneToken2Symbol,
                        Decimals = FarmTestData.SwapTokenOneToken2Decimal,
                        Address = FarmTestData.SwapTokenOneToken2ContractAddress
                    }
                }
            };
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenOneContractAddress}"] = farmTokenOne;
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenOneSymbol}"] = farmTokenOne;

        }

        private void AddFarmSwapTokenTwoOptions(FarmTokenOptions p)
        {
            var farmTokenTwo = new FarmToken
            {
                ChainName = FarmTestData.DefaultNodeName,
                Symbol = FarmTestData.SwapTokenTwoSymbol,
                Decimals = FarmTestData.SwapTokenTwoDecimal,
                Address = FarmTestData.SwapTokenTwoContractAddress,
                Type = FarmTokenType.LpToken,
                LendingPool = string.Empty,
                Tokens = new[]
                {
                    new TokenOption
                    {
                        Symbol = FarmTestData.SwapTokenTwoToken1Symbol,
                        Decimals = FarmTestData.SwapTokenTwoToken1Decimal,
                        Address = FarmTestData.SwapTokenTwoToken1ContractAddress,
                    },
                    new TokenOption
                    {
                        Symbol = FarmTestData.SwapTokenTwoToken2Symbol,
                        Decimals = FarmTestData.SwapTokenTwoToken2Decimal,
                        Address = FarmTestData.SwapTokenTwoToken2ContractAddress
                    }
                }
            };
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenTwoContractAddress}"] = farmTokenTwo;
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenTwoSymbol}"] = farmTokenTwo;
        }

        private void AddFarmSwapTokenThreeOptions(FarmTokenOptions p)
        {
            var farmTokenThree = new FarmToken
            {
                ChainName = FarmTestData.DefaultNodeName,
                Symbol = FarmTestData.SwapTokenThreeSymbol,
                Decimals = FarmTestData.SwapTokenThreeDecimal,
                Address = FarmTestData.SwapTokenThreeContractAddress,
                Type = FarmTokenType.GToken,
                LendingPool = string.Empty,
                Tokens = new[]
                {
                    new TokenOption
                    {
                        Symbol = FarmTestData.SwapTokenThreeToken1Symbol,
                        Decimals = FarmTestData.SwapTokenThreeToken1Decimal,
                        Address = FarmTestData.SwapTokenThreeToken1ContractAddress,
                    }
                }
            };
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenThreeContractAddress}"] =
                farmTokenThree;
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenThreeSymbol}"] = farmTokenThree;
        }
        
        private void AddFarmSwapTokenFourOptions(FarmTokenOptions p)
        {
            var farmTokenFour = new FarmToken
            {
                ChainName = FarmTestData.DefaultNodeName,
                Symbol = FarmTestData.SwapTokenFourSymbol,
                Decimals = FarmTestData.SwapTokenFourDecimal,
                Address = FarmTestData.SwapTokenFourContractAddress,
                Type = FarmTokenType.OtherLpToken,
                LendingPool = string.Empty
            };
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenFourContractAddress}"] = farmTokenFour;
            p.FarmTokens[$"{FarmTestData.DefaultNodeName}-{FarmTestData.SwapTokenFourSymbol}"] = farmTokenFour;

        }
    }
}