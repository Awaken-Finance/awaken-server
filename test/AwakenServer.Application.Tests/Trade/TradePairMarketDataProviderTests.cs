using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Favorite;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Tests;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;


namespace AwakenServer.Trade;

[Collection(ClusterCollection.Name)]
public class TradePairMarketDataProviderTests : TradeTestBase
{
    private readonly ITradePairAppService _tradePairAppService;
    private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<Index.TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IFavoriteAppService _favoriteAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly MockGraphQLProvider _mockGraphQLProvider;

    public TradePairMarketDataProviderTests()
    {
        _tradePairIndexRepository = GetRequiredService<INESTRepository<Index.TradePair, Guid>>();
        _tradePairSnapshotIndexRepository =
            GetRequiredService<INESTRepository<Index.TradePairMarketDataSnapshot, Guid>>();
        _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        _tradePairAppService = GetRequiredService<ITradePairAppService>();
        _tokenPriceProvider = GetRequiredService<ITokenPriceProvider>();
        _chainAppService = GetService<IChainAppService>();
        _tokenAppService = GetService<ITokenAppService>();
        _objectMapper = GetService<IObjectMapper>();
        _tradePairInfoIndex = GetService<INESTRepository<TradePairInfoIndex, Guid>>();
        _favoriteAppService = GetRequiredService<IFavoriteAppService>();
        _mockGraphQLProvider = new MockGraphQLProvider(_objectMapper, _tradePairInfoIndex, _tokenAppService);
    }
    
    [Fact]
    public async Task UpdateTotalSupplyTest()
    {
        // new snapshot
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
        {
            return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.Now.AddHours(-2),
                TotalSupply = "10"
            });
        });
        
        Thread.Sleep(3000);
        var pair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
        pair.TotalSupply.ShouldBe("10");
        
        // new snapshot but exist lastMarketData
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
        {
            return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.Now.AddHours(-1),
                LpAmount = "20"
            });
        });
        
        Thread.Sleep(3000);
        pair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
        pair.TotalSupply.ShouldBe("30");

        // merge
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
        {
            return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.Now.AddHours(-1),
                TotalSupply = "40"
            });
        });

        Thread.Sleep(3000);
        pair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
        pair.TotalSupply.ShouldBe("40");
    }
    
}