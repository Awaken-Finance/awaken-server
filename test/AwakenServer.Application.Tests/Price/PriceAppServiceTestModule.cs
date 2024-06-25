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
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",
                Decimals = 6,
                Symbol = "USDC",
                ChainId = chain.Id
            }));

            var tradePairCpuUsdt = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                    Id = Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3301"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenUSDT.Id,
                    FeeRate = 0.5
                }));
            
            var tradePairCpuUsdc = AsyncHelper.RunSync(async () => await tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = chain.Name,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                    Id = Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3302"),
                    Token0Id = tokenCPU.Id,
                    Token1Id = tokenUSDC.Id,
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
                    FeeRate = 0.03,
                }));
        }
    }
}