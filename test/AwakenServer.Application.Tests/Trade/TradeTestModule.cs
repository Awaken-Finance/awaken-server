using System.Collections.Generic;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Price;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using AwakenServer.Web3;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AwakenServer.Trade
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class TradeTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.Configure<AssetShowOptions>(o =>
            {
                o.ShowList = new List<string>()
                {
                    "ELF",
                    "USDT",
                    "BTC"
                };
            });
            context.Services.Configure<StableCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, List<Coin>>();
                o.Coins["Ethereum"] = new List<Coin>
                {
                    new Coin{Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",Symbol = "USDT"},
                    new Coin{Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365",Symbol = "USDC"},
                    new Coin{Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365",Symbol = "DAI"}
                };
                o.Coins["BSC"] = new List<Coin> {
                    new Coin{Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362",Symbol = "BUSD"},
                };
                o.Coins["AELF"] = new List<Coin> {
                    new Coin{Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA366",Symbol = "USDT"},
                };
            });

            context.Services.Configure<KLinePeriodOptions>(o =>
            {
                o.Periods = new List<int>
                {
                    60,
                    900,
                    1800,
                    3600,
                    14400,
                    86400,
                    604800
                };
            });
            
            context.Services.Configure<MainCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, Dictionary<string, Coin>>();
                o.Coins["BTC"] = new Dictionary<string, Coin>
                {
                    {"Ethereum",new Coin
                    {
                        Symbol = "BTC",
                        Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362"
                    }}
                };
            });

            context.Services.Configure<CmsOptions>(o =>
            {
                o.CmsAddress = "https://test-cms.awaken.finance/";
            });
            
            context.Services.AddSingleton<TestEnvironmentProvider>();
            context.Services.AddSingleton<IWeb3Provider, MockWeb3Provider>();
            context.Services.AddSingleton<IBlockchainClientProvider, MockWeb3Provider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainService = context.ServiceProvider.GetRequiredService<IChainAppService>();
            var tokenService = context.ServiceProvider.GetRequiredService<ITokenAppService>();
            var tradePairService = context.ServiceProvider.GetRequiredService<ITradePairAppService>();
            var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();

            var chainEth = AsyncHelper.RunSync(async ()=> await chainService.CreateAsync(new ChainCreateDto
            {
                Name = "Ethereum"
            }));
            environmentProvider.EthChainId = chainEth.Id;
            environmentProvider.EthChainName = chainEth.Name;

            var tokenETH = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                Decimals = 8,
                Symbol = "ETH",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenEthId = tokenETH.Id;
            environmentProvider.TokenEthSymbol = "ETH";
            
            var tokenUSDT = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",
                Decimals = 6,
                Symbol = "USDT",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenUsdtId = tokenUSDT.Id;
            environmentProvider.TokenUsdtSymbol = "USDT";
            
            var tokenBTC = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362",
                Decimals = 8,
                Symbol = "BTC",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenBtcId = tokenBTC.Id;
            environmentProvider.TokenBtcSymbol = "BTC";

            var tradePairEthUsdt = AsyncHelper.RunSync(async ()=> await tradePairService.CreateAsync(new TradePairCreateDto
            {
                ChainId = chainEth.Name,
                Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                Token0Id = tokenETH.Id,
                Token1Id = tokenUSDT.Id,
                FeeRate = 0.5
            }));
            environmentProvider.TradePairEthUsdtId = tradePairEthUsdt.Id;
            
            var tradePairBtcEth = AsyncHelper.RunSync(async ()=> await tradePairService.CreateAsync(new TradePairCreateDto
            {
                ChainId = chainEth.Name,
                Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                Token0Id = tokenBTC.Id,
                Token1Id = tokenETH.Id,
                FeeRate = 0.3,
            }));
            environmentProvider.TradePairBtcEthId = tradePairBtcEth.Id;
        }
    }
}