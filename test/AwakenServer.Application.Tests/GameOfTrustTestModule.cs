using System.Collections.Generic;
using AElf.EthereumNode.EventHandler.Options;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler;
using AwakenServer.Price;
using AwakenServer.Trade;
using AwakenServer.Web3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AwakenServer.Applications.GameOfTrust
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AElfEthereumEventHandlerTestBaseModule)
    )]
    public class GameOfTrustTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IDistributedEventBus, LocalDistributedEventBus>();
            context.Services.AddSingleton<IWeb3Provider,MockWeb3Provider>();
            context.Services.RemoveAll<IBlockchainClientProvider>();
            context.Services.AddSingleton<IBlockchainClientProvider,MockWeb3Provider>();
            context.Services.AddSingleton<IPriceAppService, MockPriceAppService>();
            
            
            Configure<EthereumBackgroundJobOption>(options => { options.ParallelWorker = 0; });
            Configure<AnchorCoinsOptions>(options =>
            {
                options.AnchorCoinsList = new List<AnchorCoin>
                {
                    new() {Decimal = 6, Chain = "Ethereum", Symbol = "USDT"}
                };
            });
            Configure<ISTARTokenOptions>(options =>
            {
                options.Address = "0xISTAR";
                options.Decimals = 18;
                options.Symbol = "ISTAR";
                options.InitPrice = "0.05";
            });
            context.Services.Configure<StableCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, List<Coin>>();
                o.Coins["Ethereum"] = new List<Coin>
                {
                    new Coin {Address = "0xUSDT", Symbol = "USDT"},
                    new Coin {Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365", Symbol = "USDC"},
                    new Coin {Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365", Symbol = "DAI"}
                };
                o.Coins["BSC"] = new List<Coin>
                {
                    new Coin {Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362", Symbol = "BUSD"},
                };
            });
        }
    }
}