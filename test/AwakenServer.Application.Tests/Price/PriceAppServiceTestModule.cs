using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Applications.GameOfTrust;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AwakenServer.Price
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class PriceAppServiceTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITokenPriceProvider, MockTokenPriceProvider>();
            context.Services.Configure<TokenPriceOptions>(o =>
            {
                o.PriceExpirationTimeSeconds = 2;
                o.UsdtPriceTokens = new List<string>()
                {
                    "SGR-1"
                };
                o.PriceTokenMapping = new Dictionary<string, string>()
                {
                    {"ELF", "elf-usd"},
                    {"USDT", "usdt-usd"},
                    {"ETH", "eth-usd"},
                    {"USDC", "usdc-usd"},
                    {"DAI", "dai-usd"},
                    {"BNB", "bnb-usd"},
                    {"BTC", "btc-usd"},
                    {"SGR-1", "sgr-usdt"},
                    {"TESTCACHE", "testcache-usdt"}
                };
                o.StablecoinPriority = new List<string>()
                {
                    "USDT",
                    "USDC",
                    "DAI",
                    "BUSD"
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
        }
        
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainTestHelper = context.ServiceProvider.GetRequiredService<ChainTestHelper>();
            var tokenService = context.ServiceProvider.GetRequiredService<ITokenAppService>();
            var tradePairTestHelper = context.ServiceProvider.GetRequiredService<TradePairTestHelper>();
            var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();
            var tradePairMarketDataProvider = context.ServiceProvider.GetRequiredService<ITradePairMarketDataProvider>();
            
            var chain = AsyncHelper.RunSync(async () => await chainTestHelper.CreateAsync(new ChainCreateDto
            {
                Id = "tDVV",
                Name = "tDVV"
            }));
            environmentProvider.EthChainId = chain.Id;
            environmentProvider.EthChainName = chain.Name;

            var tokenCPU = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                Decimals = 8,
                Symbol = "CPU",
                ChainId = chain.Id
            }));

            var tokenUSDT = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",
                Decimals = 6,
                Symbol = "USDT",
                ChainId = chain.Id
            }));

            var tokenREAD = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362",
                Decimals = 8,
                Symbol = "READ",
                ChainId = chain.Id
            }));
            
            
            var tokenUSDC = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA363",
                Decimals = 6,
                Symbol = "USDC",
                ChainId = chain.Id
            }));
            
            var tokenWN1 = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA364",
                Decimals = 8,
                Symbol = "SHIWN-1",
                ChainId = chain.Id
            }));
            
            var tokenWN88 = AsyncHelper.RunSync(async () => await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA365",
                Decimals = 8,
                Symbol = "SHIWN-88",
                ChainId = chain.Id
            }));

            var tradePairCpuUsdt = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3301"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenUSDT.Id,
                    Token0Symbol = tokenCPU.Symbol,
                    Token1Symbol = tokenUSDT.Symbol,
                    FeeRate = 0.5
                }));
            
            var tradePairCpuUsdc = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3302"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenUSDC.Id,
                    Token0Symbol = tokenCPU.Symbol,
                    Token1Symbol = tokenUSDC.Symbol,
                    FeeRate = 0.5
                }));
            
            var tradePairCpuRead = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA363",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3303"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenREAD.Id,
                    Token0Symbol = tokenCPU.Symbol,
                    Token1Symbol = tokenREAD.Symbol,
                    FeeRate = 0.03,
                }));
            

            var tradePairCpuWn1 = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA3634",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3304"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenWN1.Id,
                    Token0Symbol = tokenCPU.Symbol,
                    Token1Symbol = tokenWN1.Symbol,
                    FeeRate = 0.03,
                }));
            
            var tradePairCpuWn88 = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA3635",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3305"),
                    Token0Id = tokenWN1.Id,
                    Token1Id = tokenWN88.Id,
                    Token0Symbol = tokenWN1.Symbol,
                    Token1Symbol = tokenWN88.Symbol,
                    FeeRate = 0.03,
                }));

            AsyncHelper.RunSync(async () => await tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3301"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = chain.Name,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(10, 8),
                    ReserveB = NumberFormatter.WithDecimals(90, 6),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "USDT",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 1
                });
            }));
            
            AsyncHelper.RunSync(async () => await tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3302"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = chain.Name,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(10, 8),
                    ReserveB = NumberFormatter.WithDecimals(90, 6),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "USDC",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 1
                });
            }));
            
            AsyncHelper.RunSync(async () => await tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3303"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = chain.Name,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA363",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(100, 8),
                    ReserveB = NumberFormatter.WithDecimals(10, 8),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "READ",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 0
                });
            }));

            AsyncHelper.RunSync(async () => await tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3304"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = chain.Name,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA364",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(5, 8),
                    ReserveB = NumberFormatter.WithDecimals(16, 8),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "SHIWN-1",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 0
                });
            }));
            
            AsyncHelper.RunSync(async () => await tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3305"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = chain.Name,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA365",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(10, 8),
                    ReserveB = NumberFormatter.WithDecimals(6, 8),
                    BlockHeight = 101,
                    SymbolA = "SHIWN-1",
                    SymbolB = "SHIWN-88",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 0
                });
            }));
        }
    }
}