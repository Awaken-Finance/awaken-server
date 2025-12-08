using System.Collections.Generic;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.EthereumNode.EventHandler.BackgroundJob.Options;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Trade;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AwakenServer.Trade.AElf
{
    [DependsOn(
        typeof(AwakenServerContractEventHandlerCoreTestModule),
        typeof(AElfEventHandlerTestBaseModule),
        typeof(TradeTestModule)
    )]
    public class TradeProcessorTestModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
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
            
            Configure<TradePairTokenOrderOptions>(o =>
            {
                o.TradePairTokens = new List<TradePairToken>
                {
                    new TradePairToken
                    {
                        Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                        Symbol = "ELF",
                        Weight = 2
                    },
                    new TradePairToken
                    {
                        Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA368",
                        Symbol = "BTC",
                        Weight = 80
                    },
                    new TradePairToken
                    {
                        Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA366",
                        Symbol = "USDT",
                        Weight = 99
                    }
                };
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainService = context.ServiceProvider.GetRequiredService<IChainAppService>();
            var tokenService = context.ServiceProvider.GetRequiredService<ITokenAppService>();
            var tradePairService = context.ServiceProvider.GetRequiredService<ITradePairAppService>();
            var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();
            
            var chain = AsyncHelper.RunSync(async ()=> await chainService.CreateAsync(new ChainCreateDto
            {
                Name = "AELF"
            }));
            environmentProvider.AElfChainId = chain.Id;
            environmentProvider.AElfChainName = chain.Name;
            
            var tokenELF = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                Decimals = 8,
                Symbol = "ELF",
                ChainId = chain.Id
            }));
            
            var tokenBTC = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA368",
                Decimals = 8,
                Symbol = "BTC",
                ChainId = chain.Id
            }));
            
            var tokenUSDT = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA366",
                Decimals = 6,
                Symbol = "USDT",
                ChainId = chain.Id
            }));
            
            var tradePair = AsyncHelper.RunSync(async ()=> await tradePairService.CreateAsync(new TradePairCreateDto
            {
                ChainId = chain.Id,
                Address = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC",
                Token0Id = tokenELF.Id,
                Token1Id = tokenUSDT.Id,
                FeeRate = 0.5,
            }));
            environmentProvider.TradePariElfUsdtId = tradePair.Id;

        }
    }
}