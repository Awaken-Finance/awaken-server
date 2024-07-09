using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Favorite;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;
using AwakenServer;

namespace AwakenServer.Trade
{
    [Collection(ClusterCollection.Name)]
    public class TradePairAppServiceTests : TradeTestBase
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
        private readonly IClusterClient _clusterClient;
        private readonly TradePairTestHelper _tradePairTestHelper;
        

        public TradePairAppServiceTests()
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
            _clusterClient = GetRequiredService<IClusterClient>();
            _tradePairTestHelper = GetRequiredService<TradePairTestHelper>();
        }

        [Fact]
        public async Task CreateTest()
        {
            var grainResult = await _tradePairAppService.GetFromGrainAsync(Guid.NewGuid());
            grainResult.ShouldBeNull();
            
            var pairDto = new TradePairCreateDto
            {
                ChainId = ChainId,
                Address = "0xABCDEFG",
                FeeRate = 0.5,
                Token0Id = TokenBtcId,
                Token1Id = TokenUsdtId
            };
            var pair = await _tradePairTestHelper.CreateAsync(pairDto);

            var tradePair = await _tradePairAppService.GetTradePairInfoAsync(pair.Id);
            tradePair.ChainId.ShouldBe(pairDto.ChainId);
            tradePair.Address.ShouldBe(pairDto.Address);
            tradePair.FeeRate.ShouldBe(pairDto.FeeRate);
            tradePair.Token0Id.ShouldBe(pairDto.Token0Id);
            tradePair.Token1Id.ShouldBe(pairDto.Token1Id);
            
            var tradePairIndex = await _tradePairIndexRepository.GetAsync(tradePair.Id);
            tradePairIndex.ChainId.ShouldBe(pairDto.ChainId);
            tradePairIndex.Address.ShouldBe(pairDto.Address);
            tradePairIndex.FeeRate.ShouldBe(pairDto.FeeRate);
            tradePairIndex.Token0.Id.ShouldBe(pairDto.Token0Id);
            tradePairIndex.Token1.Id.ShouldBe(pairDto.Token1Id);

            var tradePairIndexFromGrain = await _tradePairAppService.GetFromGrainAsync(tradePair.Id);
            tradePairIndexFromGrain.ChainId.ShouldBe(pairDto.ChainId);
            tradePairIndexFromGrain.Address.ShouldBe(pairDto.Address);
            tradePairIndexFromGrain.FeeRate.ShouldBe(pairDto.FeeRate);
            tradePairIndexFromGrain.Token0Id.ShouldBe(pairDto.Token0Id);
            tradePairIndexFromGrain.Token1Id.ShouldBe(pairDto.Token1Id);
        }

        [Fact]
        public async Task GetTokenListTest()
        {
            var pairDto = new TradePairCreateDto
            {
                ChainId = ChainId,
                Address = "0xABCDEFG",
                FeeRate = 0.5,
                Token0Id = TokenBtcId,
                Token1Id = TokenUsdtId
            };
            await _tradePairTestHelper.CreateAsync(pairDto);

            var tokens = await _tradePairAppService.GetTokenListAsync(new GetTokenListInput
            {
                ChainId = ChainId
            });

            tokens.Token0.Count.ShouldBe(2);
            tokens.Token0.ShouldContain(t => t.Id == TokenBtcId);
            tokens.Token0.ShouldContain(t => t.Id == TokenEthId);

            tokens.Token1.Count.ShouldBe(2);
            tokens.Token1.ShouldContain(t => t.Id == TokenUsdtId);
            tokens.Token1.ShouldContain(t => t.Id == TokenEthId);
        }

        [Fact]
        public async Task GetByAddressTest()
        {
            var notExistResult = await _tradePairAppService.GetByAddressAsync(Guid.NewGuid(), "");
            notExistResult.Id.ShouldBe(Guid.Empty);
            
            var pairDto = new TradePairCreateDto
            {
                ChainId = ChainId,
                ChainName = ChainName,
                Address = "0xABCDEFG",
                FeeRate = 0.5,
                Token0Id = TokenBtcId,
                Token1Id = TokenUsdtId
            };
            var createdPair = await _tradePairTestHelper.CreateAsync(pairDto);

            var pair = await _tradePairAppService.GetByAddressAsync(pairDto.ChainName, "0x");
            pair.ShouldBeNull();

            pair = await _tradePairAppService.GetByAddressAsync(pairDto.ChainName, pairDto.Address);
            pair.ShouldNotBeNull();
            
            var pairIndexDto = await _tradePairAppService.GetByAddressAsync(createdPair.Id, pairDto.Address);
            pairIndexDto.Id.ShouldBe((createdPair.Id));
        }

        [Fact]
        public async Task TradePairSyncTest()
        {
            var syncRecordDto = new SyncRecordDto
            {
                ChainId = "tDVV",
                PairAddress = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk",
                SymbolA = "ELF",
                SymbolB = "USDT",
                ReserveA = 10000,
                ReserveB = 100,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                BlockHeight = 99
            };
            var tradePair = new Index.TradePair()
            {
                Id = TradePairEthUsdtId,
                ChainId = "tDVV",
                Address = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk",
                Token0 = new Token()
                {
                    Symbol = "ELF",
                    Decimals = 8
                },
                Token1 = new Token()
                {
                    Symbol = "USDT",
                    Decimals = 8
                }
            };
            await _tradePairIndexRepository.AddAsync(tradePair);
            await _tradePairAppService.CreateSyncAsync(syncRecordDto);
            _mockGraphQLProvider.AddSyncRecord(syncRecordDto);
            var syncList = _mockGraphQLProvider.GetSyncRecordsAsync(ChainId, 0, 100, 0, 10000);
            syncList.Result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task UpdateLiquidityTest()
        {
            var newLiquidity = new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                SymbolA = "ETH",
                SymbolB = "USDT",
                ReserveA = NumberFormatter.WithDecimals(100, 8),
                ReserveB = NumberFormatter.WithDecimals(10000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2))
            };
            
            await _tradePairAppService.CreateSyncAsync(newLiquidity);
            
            var snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(newLiquidity.Timestamp));
            
            var marketDataSnapshot = await _tradePairSnapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.TradePairId).Value(TradePairEthUsdtId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            
            marketDataSnapshot.Price.ShouldBe(100);
            marketDataSnapshot.PriceUSD.ShouldBe(100);
            marketDataSnapshot.TVL.ShouldBe(10000);
            marketDataSnapshot.ValueLocked0.ShouldBe(100);
            marketDataSnapshot.ValueLocked1.ShouldBe(10000);

            var pair = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            pair.Price.ShouldBe(100);
            pair.PriceUSD.ShouldBe(100);
            pair.TVL.ShouldBe(10000);
            pair.ValueLocked0.ShouldBe(100);
            pair.ValueLocked1.ShouldBe(10000);
            pair.PricePercentChange24h.ShouldBe(0);
            pair.TVLPercentChange24h.ShouldBe(0);

            var newLiquidity2 = new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                SymbolA = "ETH",
                SymbolB = "USDT",
                ReserveA = NumberFormatter.WithDecimals(200, 8),
                ReserveB = NumberFormatter.WithDecimals(22000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            };
            
            await _tradePairAppService.CreateSyncAsync(newLiquidity2);

            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(newLiquidity2.Timestamp));

            marketDataSnapshot = await _tradePairSnapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.TradePairId).Value(TradePairEthUsdtId)) &&
                q.Term(i => i.Field(f => f.Timestamp)
                    .Value(snapshotTime)));
            marketDataSnapshot.Price.ShouldBe(110);
            marketDataSnapshot.PriceUSD.ShouldBe(110);
            marketDataSnapshot.TVL.ShouldBe(22000);
            marketDataSnapshot.ValueLocked0.ShouldBe(200);
            marketDataSnapshot.ValueLocked1.ShouldBe(22000);

            pair = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            pair.Price.ShouldBe(110);
            pair.PriceUSD.ShouldBe(110);
            pair.TVL.ShouldBe(22000);
            pair.ValueLocked0.ShouldBe(200);
            pair.ValueLocked1.ShouldBe(22000);

            var newLiquidity3 = new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                SymbolA = "ETH",
                SymbolB = "USDT",
                ReserveA = NumberFormatter.WithDecimals(100, 8),
                ReserveB = NumberFormatter.WithDecimals(12000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1))
            };
            
            await _tradePairAppService.CreateSyncAsync(newLiquidity3);

            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(newLiquidity3.Timestamp));
            
            marketDataSnapshot = await _tradePairSnapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.TradePairId).Value(TradePairEthUsdtId)) &&
                q.Term(i => i.Field(f => f.Timestamp)
                    .Value(snapshotTime)));
            marketDataSnapshot.Price.ShouldBe(120);
            marketDataSnapshot.PriceUSD.ShouldBe(120);
            marketDataSnapshot.TVL.ShouldBe(12000);
            marketDataSnapshot.ValueLocked0.ShouldBe(100);
            marketDataSnapshot.ValueLocked1.ShouldBe(12000);

            pair = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            pair.Price.ShouldBe(110);
            pair.PriceUSD.ShouldBe(110);
            pair.TVL.ShouldBe(22000);
            pair.ValueLocked0.ShouldBe(200);
            pair.ValueLocked1.ShouldBe(22000);

            var newLiquidity4 = new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                SymbolA = "ETH",
                SymbolB = "USDT",
                ReserveA = NumberFormatter.WithDecimals(200, 8),
                ReserveB = NumberFormatter.WithDecimals(12000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1))
            };
            await _tradePairAppService.CreateSyncAsync(newLiquidity4);

            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(newLiquidity4.Timestamp));


            marketDataSnapshot = await _tradePairSnapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.TradePairId).Value(TradePairEthUsdtId)) &&
                q.Term(i => i.Field(f => f.Timestamp)
                    .Value(snapshotTime)));
            marketDataSnapshot.Price.ShouldBe(60);
            marketDataSnapshot.PriceUSD.ShouldBe(60);
            marketDataSnapshot.TVL.ShouldBe(12000);
            marketDataSnapshot.ValueLocked0.ShouldBe(200);
            marketDataSnapshot.ValueLocked1.ShouldBe(12000);
            marketDataSnapshot.PriceHigh.ShouldBe(120);
            marketDataSnapshot.PriceLow.ShouldBe(60);

            pair = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            pair.Price.ShouldBe(110);
            pair.PriceUSD.ShouldBe(110);
            pair.TVL.ShouldBe(22000);
            pair.ValueLocked0.ShouldBe(200);
            pair.ValueLocked1.ShouldBe(22000);
        }

        [Fact]
        public async Task UpdateTradePairTest()
        {
            var tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            tradePair.FeePercent7d.ShouldBe(0);

            await InitTradePairAsync();

            var snapshot = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                Volume = 2000,
                PriceHigh = 100,
                PriceLow = 80,
                TradeCount = 3,
                TradeValue = 20000,
                TradeAddressCount24h = 3,
                TVL = 50000,
                PriceUSD = 1.5,
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot);

            var snapshot2 = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddHours(-3),
                Volume = 2200,
                PriceHigh = 120,
                PriceLow = 100,
                TradeCount = 3,
                TradeValue = 24000,
                TradeAddressCount24h = 2,
                TVL = 60000,
                PriceUSD = 2,
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot2);

            var snapshot3 = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Volume = 2200,
                PriceHigh = 130,
                PriceLow = 110,
                TradeCount = 3,
                TradeValue = 24000,
                TradeAddressCount24h = 3,
                TVL = 60000,
                PriceUSD = 2,
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot3);

            var snapshot4 = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddDays(-3),
                Volume = 2200,
                PriceHigh = 130,
                PriceLow = 110,
                TradeCount = 3,
                TradeValue = 24000,
                TradeAddressCount24h = 3,
                TVL = 60000,
                PriceUSD = 2,
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot4);
            
            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);

            tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            // tradePair.FeePercent7d.ShouldBe(6400 * 2 * 0.5 * 365 * 100 / (400000 * 7));

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);
            tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            // tradePair.FeePercent7d.ShouldBe(6400 * 2 * 0.5 * 365 * 100 / (400000 * 7));

            await _tradePairAppService.UpdateTradePairAsync(Guid.NewGuid());
        }

        [Fact]
        public async Task UpdateTradePairFromGrainTest()
        {
            var tradePair = await _tradePairAppService.GetFromGrainAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            tradePair.FeePercent7d.ShouldBe(0);

            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = ChainName,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow.AddDays(-4),
                    Price = 2,
                    ValueLocked0 = 100000,
                    ValueLocked1 = 200000
                });
            });
            
            tradePair = await _tradePairAppService.GetFromGrainAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.ValueLocked0.ShouldBe(100000);
            tradePair.ValueLocked1.ShouldBe(200000);
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = ChainName,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                    Volume = 2000,
                    Price = 100,
                    PriceUSD = 100,
                    TradeCount = 3,
                    TradeValue = 20000,
                    TradeAddressCount24h = 3,
                    TVL = 50000,
                });
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = ChainName,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow.AddHours(-3),
                    Volume = 2200,
                    Price = 100,
                    PriceUSD = 100,
                    TradeCount = 3,
                    TradeValue = 24000,
                    TradeAddressCount24h = 2,
                    TVL = 60000,
                });
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = ChainName,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Volume = 2200,
                    Price = 140,
                    PriceUSD = 140,
                    TradeCount = 3,
                    TradeValue = 24000,
                    TradeAddressCount24h = 3,
                    TVL = 60000,
                });
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = ChainName,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow.AddDays(-3),
                    Volume = 2200,
                    Price = 2,
                    PriceUSD = 2,
                    TradeCount = 3,
                    TradeValue = 24000,
                    TradeAddressCount24h = 3,
                    TVL = 60000,
                });
            });

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);
            
            tradePair = await _tradePairAppService.GetFromGrainAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(140);
            tradePair.PricePercentChange24h.ShouldBe(40d);
            tradePair.Volume24h.ShouldBe(4400);
            tradePair.VolumePercentChange24h.ShouldBe(120);
            tradePair.PriceHigh24h.ShouldBe(140);
            tradePair.PriceLow24h.ShouldBe(100);
            tradePair.TradeCount24h.ShouldBe(6);
            tradePair.TradeValue24h.ShouldBe(48000);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(200000d);
            tradePair.TVLPercentChange24h.ShouldBe(300d);
            tradePair.PriceUSD.ShouldBe(140);
            //tradePair.FeePercent7d.ShouldBe( 6400 *2 * 0.5 * 365 * 100 / (400000 * 7));
        }

        [Fact]
        public async Task UpdateTradePair_NoLastDaySnapshot_Test()
        {
            await InitTradePairAsync();
            var snapshot = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Volume = 12000,
                PriceHigh = 120,
                PriceLow = 80,
                TradeCount = 3,
                TradeValue = 120000,
                TVL = 50000,
                PriceUSD = 1.5
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot);

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);

            var tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            //tradePair.FeePercent7d.ShouldBe((12000* tradePair.FeeRate * 2 * 365 * 100) / (400000 * 7));

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);
        }

        [Fact]
        public async Task UpdateTradePair_NoNeedUpdate_Test()
        {
            await InitTradePairAsync();

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);

            var tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            tradePair.FeePercent7d.ShouldBe(0);

            var snapshot = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                Volume = 12000,
                PriceHigh = 120,
                PriceLow = 80,
                TradeCount = 3,
                TradeValue = 120000,
                TVL = 50000,
                PriceUSD = 1.5
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot);

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);

            tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(2);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            tradePair.FeePercent7d.ShouldBe(0);
        }

        [Fact]
        public async Task UpdateTradePair_TVL_Test()
        {
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenEthId, TokenUsdtId, 2);
            var tradePairIndex = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            tradePairIndex.Price = 0;
            tradePairIndex.ValueLocked0 = 0;
            tradePairIndex.ValueLocked1 = 0;
            await _tradePairIndexRepository.UpdateAsync(tradePairIndex);

            var snapshot = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Volume = 12000,
                PriceHigh = 120,
                PriceLow = 80,
                TradeCount = 3,
                TradeValue = 120000,
                TVL = 0,
                PriceUSD = 1.5
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot);

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);

            var tradePair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
            tradePair.Price.ShouldBe(0);
            tradePair.PricePercentChange24h.ShouldBe(0);
            tradePair.Volume24h.ShouldBe(0);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            tradePair.PriceHigh24h.ShouldBe(0);
            tradePair.PriceLow24h.ShouldBe(0);
            tradePair.TradeCount24h.ShouldBe(0);
            tradePair.TradeValue24h.ShouldBe(0);
            tradePair.TradeAddressCount24h.ShouldBe(0);
            tradePair.TVL.ShouldBe(0);
            tradePair.TVLPercentChange24h.ShouldBe(0);
            tradePair.PriceUSD.ShouldBe(0);
            tradePair.FeePercent7d.ShouldBe(0);

            await _tradePairAppService.UpdateTradePairAsync(TradePairEthUsdtId);
        }

        [Fact]
        public async Task UpdateTradePair_Token1Price_Test()
        {
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenEthId, TokenUsdtId, 2);
            var tradePairIndex = await _tradePairIndexRepository.GetAsync(TradePairBtcEthId);
            tradePairIndex.Price = 2;
            tradePairIndex.ValueLocked0 = 100000;
            tradePairIndex.ValueLocked1 = 200000;
            await _tradePairIndexRepository.UpdateAsync(tradePairIndex);

            var snapshot = new Index.TradePairMarketDataSnapshot(Guid.NewGuid())
            {
                ChainId = ChainId,
                TradePairId = TradePairBtcEthId,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Volume = 2000,
                PriceHigh = 100,
                PriceLow = 80,
                TradeCount = 3,
                TradeValue = 20000,
                TradeAddressCount24h = 3,
                TVL = 50000,
                PriceUSD = 1.5,
            };
            await _tradePairSnapshotIndexRepository.AddAsync(snapshot);

            await _tradePairAppService.UpdateTradePairAsync(TradePairBtcEthId);

            var tradePair = await _tradePairAppService.GetAsync(TradePairBtcEthId);
            tradePair.PriceUSD.ShouldBe(0);
        }

        private async Task InitTradePairAsync()
        {
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenEthId, TokenUsdtId, 2);
            var tradePairIndex = await _tradePairIndexRepository.GetAsync(TradePairEthUsdtId);
            tradePairIndex.Price = 2;
            tradePairIndex.ValueLocked0 = 100000;
            tradePairIndex.ValueLocked1 = 200000;
            var tradePairInfo = new TradePairInfoIndex()
            {
                Id = TradePairEthUsdtId,
            };
            var grain = Cluster.Client.GetGrain<ITradePairGrain>(
                GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
            await grain.AddOrUpdateAsync(_objectMapper.Map<Index.TradePair, TradePairGrainDto>(tradePairIndex));
            await _tradePairIndexRepository.UpdateAsync(tradePairIndex);
        }

        [Fact]
        public async Task GetFromGrainTest()
        {
            var pair = await _tradePairAppService.GetFromGrainAsync(TradePairEthUsdtId);
            pair.FeeRate.ShouldBe(0.0005);
            pair.Address.ShouldBe("0xPool006a6FaC8c710e53c4B2c2F96477119dA361");
            pair.Token0Id.ShouldBe(TokenEthId);
            pair.Token1Id.ShouldBe(TokenUsdtId);
            pair.ChainId.ShouldBe(ChainName);
        }
        
        [Fact]
        public async Task GetByIdsTest()
        {
            var emptyResult = await _tradePairAppService.GetByIdsAsync(new GetTradePairByIdsInput());
            emptyResult.Items.Count.ShouldBe(0);
            
            var pairs = await _tradePairAppService.GetByIdsAsync(new GetTradePairByIdsInput
            {
                Ids = new List<Guid> { TradePairEthUsdtId }
            });

            var ethPair = await _tradePairAppService.GetAsync(TradePairEthUsdtId);

            pairs.Items.Count.ShouldBe(1);
            pairs.Items[0].ShouldBeEquivalentTo(ethPair);

            pairs = await _tradePairAppService.GetByIdsAsync(new GetTradePairByIdsInput
            {
                Ids = new List<Guid> { TradePairEthUsdtId, TradePairBtcEthId }
            });
            var btcPair = await _tradePairAppService.GetAsync(TradePairBtcEthId);
            pairs.Items.Count.ShouldBe(2);
            pairs.Items[0].ShouldBeEquivalentTo(btcPair);
            pairs.Items[1].ShouldBeEquivalentTo(ethPair);
        }

        [Fact]
        public async Task GetListTest()
        {
            var tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                MaxResultCount = 10
            });

            tradePairs.TotalCount.ShouldBe(3);
            tradePairs.Items.Count.ShouldBe(3);
            var ids = new List<Guid> { TradePairBtcEthId, TradePairEthUsdtId };
            tradePairs.Items[0].Id.ShouldBeOneOf(ids.ToArray());
            tradePairs.Items[0].Id.ShouldBeOneOf(ids.ToArray());

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                FeeRate = 0.0005,
                MaxResultCount = 10
            });
            tradePairs.TotalCount.ShouldBe(1);
            tradePairs.Items.Count.ShouldBe(1);
            tradePairs.Items[0].Id.ShouldBe(TradePairEthUsdtId);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                TokenSymbol = "ETH"
            });

            tradePairs.TotalCount.ShouldBe(2);
            tradePairs.Items.Count.ShouldBe(2);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                TokenSymbol = "BTC"
            });

            tradePairs.TotalCount.ShouldBe(2);
            tradePairs.Items.Count.ShouldBe(2);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "price"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volumepercentchange24h"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricehigh24h"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricelow24h"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "feepercent7d"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tvl"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricepercentchange24h"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volume24h"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tradepair"
            });
            tradePairs.TotalCount.ShouldBe(3);
            tradePairs.Items[0].Id.ShouldBe(TradePairBtcEthId);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "price asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volumepercentchange24h asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricehigh24h asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricelow24h asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "feepercent7d asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tvl asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricepercentchange24h asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volume24h asc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tradepair asc"
            });
            tradePairs.TotalCount.ShouldBe(3);
            tradePairs.Items[0].Id.ShouldBe(TradePairBtcEthId);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "price dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volumepercentchange24h dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricehigh24h dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricelow24h dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "feepercent7d dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tvl dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "pricepercentchange24h dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "volume24h dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Sorting = "tradepair dsc"
            });
            tradePairs.TotalCount.ShouldBe(3);
            tradePairs.Items[0].Id.ShouldBe(TradePairEthUsdtId);

            //fav
            await _favoriteAppService.CreateAsync(new FavoriteCreateDto
            {
                Address = "Y1N9mXz8sw28mMvWMuCyocCf2qkoZZTFT9mdpB2KBTqT5R1fN",
                TradePairId = TradePairEthUsdtId,
            });
            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Page = TradePairPage.MarketPage,
                TradePairFeature = TradePairFeature.Fav,
                Address = "Y1N9mXz8sw28mMvWMuCyocCf2qkoZZTFT9mdpB2KBTqT5R1fN",
            });
            tradePairs.TotalCount.ShouldBe(1);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Page = TradePairPage.TradePage,
                TradePairFeature = TradePairFeature.OtherSymbol
            });
            tradePairs.TotalCount.ShouldBe(3);

            tradePairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainName,
                Token0Id = Guid.Parse("ad51e98e-51b0-45df-870d-348d62802a04"),
                Token0Symbol = "BTC",
                Token1Id = Guid.Parse("8e1b0ebc-5fef-4846-b619-4444887125bb"),
                Token1Symbol = "ETH",
                SearchTokenSymbol = "BTC",
                TradePairFeature = TradePairFeature.Fav,
                Address = ""
            });
            tradePairs.TotalCount.ShouldBe(0);
        }

        [Fact]
        public async Task GetTokenAsyncTest()
        {
            /*var tokenETH = AsyncHelper.RunSync(async ()=> await tokenService.CreateAsync(new TokenCreateDto
            {
                Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA360",
                Decimals = 8,
                Symbol = "ETH",
                ChainId = chainEth.Id
            }));*/
            var tokenExist = await _tokenAppService.GetAsync(new GetTokenInput
            {
                Id = TokenEthId
            });

            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = tokenExist.ChainId,
                Address = tokenExist.Address,
            });
            token.ShouldNotBeNull();
        }

        [Fact]
        public async Task SyncTradePair_Test()
        {
            var newToken = await _tradePairAppService.SyncTokenAsync(ChainId, "NewToken", new ChainDto
            {
                Name = ChainName,
                Id = ChainId
            });
            newToken.Symbol.ShouldBe("NewToken");
            
            var id = Guid.NewGuid();
            var TradePairInfoDto = new TradePairInfoDto
            {
                Id = id.ToString(),
                ChainId = ChainName,
                Token0Symbol = "ETH",
                Token1Symbol = "USDT",
                FeeRate = 0.5,
                Address = "rCdkUKwrcUPm4F7Ek71pzJ8xoWRQ1YRdLo9i7wJ1XutSY5pjG"
            };
            _mockGraphQLProvider.AddTradePairInfoAsync(TradePairInfoDto);
            var chains = await _chainAppService.GetListAsync(new GetChainInput());
            foreach (var chain in chains.Items)
            {
                var result = await _mockGraphQLProvider.GetTradePairInfoListLocalAsync(new GetTradePairsInfoInput
                {
                    ChainId = chain.Name
                });
                foreach (var pair in result.TradePairInfoDtoList.Data)
                {
                    await _tradePairAppService.SyncTokenAsync(pair.ChainId, pair.Token0Symbol, chain);
                    await _tradePairAppService.SyncTokenAsync(pair.ChainId, pair.Token1Symbol, chain);
                    await _tradePairAppService.SyncPairAsync(0,pair, chain);
                }

                // token需要有2个，tradePair需要有1个
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = TradePairInfoDto.Token0Symbol
                });
                token.ShouldNotBeNull();
                token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = TradePairInfoDto.Token1Symbol
                });
                token.ShouldNotBeNull();
                
                var tradePairFromGrain = await _tradePairAppService.GetFromGrainAsync(Guid.Parse(TradePairInfoDto.Id));
                tradePairFromGrain.ShouldNotBeNull();
                
                var tradePair = await _tradePairAppService.GetAsync(Guid.Parse(TradePairInfoDto.Id));
                tradePair.ShouldNotBeNull();

                
            }

            return;
        }
        
        [Fact]
        public async Task GetTradePairByIdsTest()
        {
            var result = await _tradePairAppService.GetByIdsFromGrainAsync(new GetTradePairByIdsInput
            {
                Ids = new List<Guid>
                {
                    TradePairEthUsdtId,
                    TradePairBtcEthId
                }
            });
            
            result.Items.Count.ShouldBe(2);
            result.Items[0].Token0.Symbol.ShouldBe("ETH");
            result.Items[0].Token1.Symbol.ShouldBe("USDT");
            result.Items[1].Token0.Symbol.ShouldBe("BTC");
            result.Items[1].Token1.Symbol.ShouldBe("ETH");
        }
        
        
        [Fact]
        public async Task RevertTest()
        {
            await _tradePairAppService.RevertTradePairAsync(ChainId);
            
            var newPairDto = new TradePairInfoDto
            {
                ChainId = "tDVV",
                BlockHeight = 99,
                TransactionHash = "0x1",
                Token0Symbol = "BTC",
                Token1Symbol = "USDT",
                Id = Guid.NewGuid().ToString(),
                Address = "0x2"
            };
            await _tradePairAppService.SyncPairAsync(0, newPairDto, new ChainDto
            {
                Name = ChainId,
                Id = ChainId
            });
            Thread.Sleep(3000);
            var result = await _tradePairAppService.GetAsync(Guid.Parse(newPairDto.Id));
            result.Id.ShouldBe(Guid.Parse(newPairDto.Id));
            
            await _tradePairAppService.DoRevertAsync(ChainId, new List<string>
            {
                newPairDto.TransactionHash
            });
            Thread.Sleep(3000);
            
            var pairResult = await _tradePairAppService.GetTradePairAsync(ChainName, newPairDto.Address);
            pairResult.ShouldBeNull();
        }
    }
}