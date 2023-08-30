using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.Trade.Handlers
{
    public class NewLiquidityHandlerTest : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly INESTRepository<Index.TradePairMarketDataSnapshot, Guid> _nestRepository;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;

        public NewLiquidityHandlerTest()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _nestRepository = GetRequiredService<INESTRepository<Index.TradePairMarketDataSnapshot, Guid>>();
            _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        }

        [Fact]
        public async Task HandleEventTest()
        {
            var eventData = new NewLiquidityRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Type = LiquidityType.Mint,
                Timestamp = DateTime.UtcNow,
                Token0Amount = "100",
                Token1Amount = "1000",
                LpTokenAmount = "10000",
                TransactionHash = "tx"
            };
            await _eventBus.PublishAsync(eventData);

            var snapshotTime = eventData.Timestamp.Date.AddHours(eventData.Timestamp.Hour);
            var marketData = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(eventData.ChainId, eventData.TradePairId, snapshotTime);
            marketData.Timestamp.ShouldBe(snapshotTime);
            marketData.TotalSupply.ShouldBe(eventData.LpTokenAmount);
            
            var marketDataSnapshots = await _nestRepository.GetListAsync(q =>
                    q.Term(i => i.Field(f => f.ChainId).Value(eventData.ChainId)) &&
                    q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)));
            marketDataSnapshots.Item2.Count.ShouldBe(1);
            marketDataSnapshots.Item2.Last().Timestamp.ShouldBe(snapshotTime);
            marketDataSnapshots.Item2.Last().TotalSupply.ShouldBe(eventData.LpTokenAmount);
            
            eventData.Type = LiquidityType.Mint;
            eventData.Timestamp = eventData.Timestamp.AddHours(1);
            eventData.LpTokenAmount = "2000";
            await _eventBus.PublishAsync(eventData);
            
            snapshotTime = eventData.Timestamp.Date.AddHours(eventData.Timestamp.Hour);
            marketData = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(eventData.ChainId, eventData.TradePairId, snapshotTime);
            marketData.Timestamp.ShouldBe(snapshotTime);
            marketData.TotalSupply.ShouldBe("12000");
            
            marketDataSnapshots = await _nestRepository.GetListAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(eventData.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)), sortExp: s => s.Timestamp);
            marketDataSnapshots.Item2.Count.ShouldBe(2);
            marketDataSnapshots.Item2.Last().Timestamp.ShouldBe(snapshotTime);
            marketDataSnapshots.Item2.Last().TotalSupply.ShouldBe("12000");
            
            eventData.Type = LiquidityType.Burn;
            eventData.Timestamp = eventData.Timestamp.AddHours(1);
            eventData.LpTokenAmount = "4000";
            await _eventBus.PublishAsync(eventData);
            
            snapshotTime = eventData.Timestamp.Date.AddHours(eventData.Timestamp.Hour);
            marketData = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(eventData.ChainId, eventData.TradePairId, snapshotTime);
            marketData.Timestamp.ShouldBe(snapshotTime);
            marketData.TotalSupply.ShouldBe("8000");
            
            marketDataSnapshots = await _nestRepository.GetListAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(eventData.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)), sortExp: s => s.Timestamp);
            marketDataSnapshots.Item2.Count.ShouldBe(3);
            marketDataSnapshots.Item2.Last().Timestamp.ShouldBe(snapshotTime);
            marketDataSnapshots.Item2.Last().TotalSupply.ShouldBe("8000");
            
            var eventData2 = new NewLiquidityRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Type = LiquidityType.Burn,
                Timestamp = eventData.Timestamp.AddHours(-1),
                Token0Amount = "100",
                Token1Amount = "1000",
                LpTokenAmount = "1000",
                TransactionHash = "tx"
            };
            await _eventBus.PublishAsync(eventData2);
            
            snapshotTime = eventData.Timestamp.Date.AddHours(eventData.Timestamp.Hour);
            marketData = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(eventData.ChainId, eventData.TradePairId, snapshotTime);
            marketData.Timestamp.ShouldBe(snapshotTime);
            marketData.TotalSupply.ShouldBe("7000");
            
            marketDataSnapshots = await _nestRepository.GetListAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(eventData2.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(eventData2.TradePairId)), sortExp: s => s.Timestamp);
            marketDataSnapshots.Item2.Count.ShouldBe(3);
            marketDataSnapshots.Item2.Last().Timestamp.ShouldBe(snapshotTime);
            marketDataSnapshots.Item2.Last().TotalSupply.ShouldBe("7000");
        }
    }
}