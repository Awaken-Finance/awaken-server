using System;
using System.Collections.Generic;
using AElf.Types;
using AwakenServer.AetherLinkApi;
using AwakenServer.Chains;
using AwakenServer.CoinGeckoApi;
using AwakenServer.Grains.Grain.Chain;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Tokens;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AwakenServer.Grains.Tests;

[DependsOn(
    typeof(AwakenServerGrainsModule),
    typeof(AwakenServerDomainTestModule),
    typeof(AwakenServerDomainModule),
    typeof(AwakenServerAetherLinkApiModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class AwakenServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<TestEnvironmentProvider>();
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<CoinGeckoOptions>(o =>
        {
            o.CoinIdMapping = new Dictionary<string, string>
            {
                { "ELF", "aelf" },
                { "NO-PRICE", "NO-PRICE" }
            };
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();

        var chainGrain = clusterClient.GetGrain<IChainGrain>("tDVV");
        var chain = AsyncHelper.RunSync(async () => await chainGrain.AddChainAsync(new ChainGrainDto()
        {
            Id = "tDVV",
            Name = "tDVV"
        }));
        environmentProvider.EthChainId = chain.Data.Id;
        environmentProvider.EthChainName = chain.Data.Name;

        var token0 = new TokenDto()
        {
            Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
            Id = environmentProvider.TokenEthId,
            Decimals = 8,
            Symbol = "ETH"
        };
        
        var token1 = new TokenDto()
        {
            Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA361",
            Id = environmentProvider.TokenUsdtId,
            Decimals = 6,
            Symbol = "USDT"
        };
        
        environmentProvider.TokenEthId = Guid.NewGuid();
        AsyncHelper.RunSync(async () => await clusterClient.GetGrain<ITokenStateGrain>(environmentProvider.TokenEthId).CreateAsync(new TokenCreateDto()
        {
            Id = environmentProvider.TokenEthId,
            ChainId = environmentProvider.EthChainId,
            Address = token0.Address,
            Symbol = token0.Symbol,
            Decimals = token0.Decimals,
        }));
        environmentProvider.TokenEthSymbol = token0.Symbol;

        environmentProvider.TokenUsdtId = Guid.NewGuid();
        AsyncHelper.RunSync(async () => await clusterClient.GetGrain<ITokenStateGrain>(environmentProvider.TokenUsdtId).CreateAsync(new TokenCreateDto()
        {
            Id = environmentProvider.TokenUsdtId,
            ChainId = environmentProvider.EthChainId,
            Address = token1.Address,
            Symbol = token1.Symbol,
            Decimals = token1.Decimals,
        }));
        environmentProvider.TokenUsdtSymbol = token1.Symbol;

        
        environmentProvider.TradePairEthUsdtId = Guid.NewGuid();
        var grain = clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(environmentProvider.TradePairEthUsdtId));

        environmentProvider.TradePairEthUsdtAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361";
        AsyncHelper.RunSync(async () => await grain.AddOrUpdateAsync(new TradePairGrainDto()
        {
            Id = environmentProvider.TradePairEthUsdtId,
            ChainId = chain.Data.Name,
            Address = environmentProvider.TradePairEthUsdtAddress,
            Token0 = token0,
            Token1 = token1,
            Token0Id = environmentProvider.TokenEthId,
            Token1Id = environmentProvider.TokenUsdtId,
            FeeRate = 0.5
        }));

        AsyncHelper.RunSync(async () => grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto()
        {
            Id = Guid.NewGuid(),
            ChainId = environmentProvider.EthChainId,
            TradePairId = environmentProvider.TradePairEthUsdtId,
            Timestamp = DateTime.Now,
            TotalSupply = "100000",
        }));

        AsyncHelper.RunSync(async () => grain.UpdatePriceAsync(new SyncRecordGrainDto()
        {
            ChainId = environmentProvider.EthChainId,
            PairAddress = environmentProvider.TradePairEthUsdtAddress,
            SymbolA = "ETH",
            SymbolB = "USDT",
            ReserveA = 100,
            ReserveB = 1000,
            Timestamp = DateTime.Now.Microsecond,
        }));
        
    }
}