using System;
using System.Collections.Generic;
using AwakenServer.Applications.GameOfTrust;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.StatInfo;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using AwakenServer.Worker;
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
            context.Services.AddSingleton<IPriceAppService, MockPriceAppService>();
            context.Services.AddSingleton<IRevertProvider, RevertProvider>();
            context.Services.Configure<TradeRecordRevertWorkerSettings>(o =>
            {
                o.QueryOnceLimit = 1;
                o.BlockHeightLimit = 100;
                o.RetryLimit = 2;
                o.TransactionHashExpirationTime = 360;
                o.TimePeriod = 75000;
            });
            

            context.Services.Configure<AssetWhenNoTransactionOptions>(o =>
            {
                o.Symbols = new List<string>
                {
                    "USDT",
                    "BTC",
                    "ETH",
                    "no"
                };
                o.ExpireDurationMinutes = 1;
                o.ContractAddressOfGetBalance = new Dictionary<string, string>
                    { { "eos", "test" }, { "tDVV", "TEST" }, { "CAElf", "test" } };
            });
            context.Services.Configure<AssetShowOptions>(o =>
            {
                o.ShowList = new List<string>()
                {
                    "ELF",
                    "USDT",
                    "BTC"
                };
                o.NftList = new List<string>()
                {
                    "ELF",
                    "USDT",
                    "SGR-1",
                    "ELEPHANT-1",
                    "USDC",
                    "ETH",
                    "BNB",
                    "DAI"
                };
                o.ShowListLength = 6;
                o.TransactionFee = 1;
                o.DefaultSymbol = "BTC";
            });
            context.Services.Configure<StableCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, List<Coin>>();
                o.Coins["tDVV"] = new List<Coin>
                {
                    new Coin { Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361", Symbol = "USDT" },
                    new Coin { Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365", Symbol = "USDC" },
                    new Coin { Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365", Symbol = "DAI" }
                };
                o.Coins["BSC"] = new List<Coin>
                {
                    new Coin { Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362", Symbol = "BUSD" },
                };
                o.Coins["AELF"] = new List<Coin>
                {
                    new Coin { Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA366", Symbol = "USDT" },
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
            
            context.Services.Configure<StatInfoOptions>(o =>
            {
                o.Periods = new List<int>
                {
                    3600,
                    21600,
                    86400,
                    604800
                };
                o.TypePeriodMapping = new Dictionary<string, long>()
                {
                    { "Day", 3600 },
                    { "Week", 21600 },
                    { "Month", 86400 },
                    { "Year", 604800 }
                };
                o.DataVersion = "v1";
            });

            context.Services.Configure<MainCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, Dictionary<string, Coin>>();
                o.Coins["BTC"] = new Dictionary<string, Coin>
                {
                    {
                        "tDVV", new Coin
                        {
                            Symbol = "BTC",
                            Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362"
                        }
                    }
                };
            });

            context.Services.Configure<CmsOptions>(o => { o.CmsAddress = "https://test-cms.awaken.finance/"; });

            context.Services.Configure<ContractsTokenOptions>(o =>
            {
                o.Contracts = new Dictionary<string, string>
                {
                    { "0.0005", "2F4vThkqXxzoUGQowUzmGNQwyGc6a6Ca7UZK5eWHpwmkwRuUpN" },
                    { "0.001", "2KRHY1oZv5S28YGRJ3adtMxfAh7WQP3wmMyoFq33oTc7Mt5Z1Y" },
                    { "0.03", "UoHeeCXZ6fV481oD3NXASSexWVtsPLgv2Wthm3BGrPAgqdS5d" },
                    { "0.05", "2tWvBTmX7YhB2HLcWGGG5isVCgab96jdaXnqDs1jzSsyqwmjic" }
                };
            });

            context.Services.AddSingleton<TestEnvironmentProvider>();
            
            context.Services.AddSingleton<IBlockchainClientProvider, MockEthereumClientProvider>();
            context.Services.AddSingleton<IBlockchainClientProvider, MockTDVVClientProvider>();
            context.Services.AddSingleton<IBlockchainClientProvider, MockDefaultClientProvider>(); 

            context.Services.AddSingleton<ITradePairMarketDataProvider, TradePairMarketDataProvider>();
            context.Services.AddSingleton<ITradeRecordAppService, TradeRecordAppService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainTestHelper = context.ServiceProvider.GetRequiredService<ChainTestHelper>();
            var tokenService = context.ServiceProvider.GetRequiredService<ITokenAppService>();
            var tradePairTestHelper = context.ServiceProvider.GetRequiredService<TradePairTestHelper>();
            var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();

            var chainEth = AsyncHelper.RunSync(async () => await chainTestHelper.CreateAsync(new ChainCreateDto
            {
                Id = "tDVV",
                Name = "tDVV"
            }));
            environmentProvider.EthChainId = chainEth.Id;
            environmentProvider.EthChainName = chainEth.Name;

            var tokenETH = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                Decimals = 8,
                Symbol = "ETH",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenEthId = tokenETH.Id;
            environmentProvider.TokenEthSymbol = "ETH";

            var tokenUSDT = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",
                Decimals = 6,
                Symbol = "USDT",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenUsdtId = tokenUSDT.Id;
            environmentProvider.TokenUsdtSymbol = "USDT";

            var tokenBTC = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362",
                Decimals = 8,
                Symbol = "BTC",
                ChainId = chainEth.Id
            }));
            environmentProvider.TokenBtcId = tokenBTC.Id;
            environmentProvider.TokenBtcSymbol = "BTC";

            var tradePairEthUsdt = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chainEth.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                    Id = Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3301"),
                    Token0Id = tokenETH.Id,
                    Token1Id = tokenUSDT.Id,
                    FeeRate = 0.0005
                }));
            environmentProvider.TradePairEthUsdtId = tradePairEthUsdt.Id;
            environmentProvider.TradePairEthUsdtAddress = tradePairEthUsdt.Address;
            
            var tradePairBtcEth = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chainEth.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3302"),
                    Token0Id = tokenBTC.Id,
                    Token1Id = tokenETH.Id,
                    FeeRate = 0.03,
                }));
            environmentProvider.TradePairBtcEthId = tradePairBtcEth.Id;
            environmentProvider.TradePairBtcEthAddress = tradePairBtcEth.Address;
            
            
            var tradePairBtcUsdt = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chainEth.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA363",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3303"),
                    Token0Id = tokenBTC.Id,
                    Token1Id = tokenUSDT.Id,
                    FeeRate = 0.03,
                }));
            environmentProvider.tradePairBtcUsdtId = tradePairBtcUsdt.Id;
            environmentProvider.tradePairBtcUsdtAddress = tradePairBtcUsdt.Address;
        }
    }
}