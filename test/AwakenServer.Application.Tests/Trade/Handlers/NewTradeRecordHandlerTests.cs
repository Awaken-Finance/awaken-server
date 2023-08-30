using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.Trade.Handlers
{
    public class NewTradeRecordHandlerTests : TradeTestBase
    {
        private readonly INESTRepository<Index.TradePairMarketDataSnapshot, Guid> _snapshotIndexRepository;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;

        public NewTradeRecordHandlerTests()
        {
            _snapshotIndexRepository =
                GetRequiredService<INESTRepository<Index.TradePairMarketDataSnapshot, Guid>>();
            _tradePairIndexRepository =
                GetRequiredService<INESTRepository<Index.TradePair, Guid>>();
            _tradeRecordAppService = GetRequiredService<ITradeRecordAppService>();
            _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        }

        [Fact]
        public async Task HandleEventTest()
        {
            var recordInput = new TradeRecordCreateDto()
            {
                ChainId = ChainId,
                Address = "0x",
                Side = TradeSide.Buy,
                Token0Amount = "1000",
                Token1Amount = "2000",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2)),
                TransactionHash = "tx",
                TradePairId = TradePairEthUsdtId
            };
            await _tradeRecordAppService.CreateAsync(recordInput);

            var snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));
            // var marketData = await _tradePairMarketDataSnapshotRepository.GetAsync(m =>
            //     m.ChainId == recordInput.ChainId && m.TradePairId == recordInput.TradePairId && m.Timestamp == snapshotTime);
            // marketData.Volume.ShouldBe(1000);
            // marketData.TradeValue.ShouldBe(2000);
            // marketData.TradeCount.ShouldBe(1);
            // marketData.TradeAddressCount24h.ShouldBe(1);
            
            var marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(1000);
            marketDataSnapshot.TradeValue.ShouldBe(2000);
            marketDataSnapshot.TradeCount.ShouldBe(1);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);
            
            var tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(1000);
            tradePair.TradeValue24h.ShouldBe(2000);
            tradePair.TradeCount24h.ShouldBe(1);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            
            recordInput.Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddHours(-1));
            recordInput.Token0Amount = "1000";
            recordInput.Token1Amount = "2000";
            
            await _tradeRecordAppService.CreateAsync(recordInput);
            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));
            
            // marketData = await _tradePairMarketDataSnapshotRepository.GetAsync(m =>
            //     m.ChainId == recordInput.ChainId && m.TradePairId == recordInput.TradePairId && m.Timestamp == snapshotTime);
            // marketData.Volume.ShouldBe(200);
            // marketData.TradeValue.ShouldBe(300);
            // marketData.TradeCount.ShouldBe(1);
            // marketData.TradeAddressCount24h.ShouldBe(1);
            
            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(1000);
            marketDataSnapshot.TradeValue.ShouldBe(2000);
            marketDataSnapshot.TradeCount.ShouldBe(1);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);
            
            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(1000);
            tradePair.TradeValue24h.ShouldBe(2000);
            tradePair.TradeCount24h.ShouldBe(1);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(0);
            
            recordInput.Token0Amount = "1000";
            recordInput.Token1Amount = "2000";
            
            await _tradeRecordAppService.CreateAsync(recordInput);
            
            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));
            // marketData = await _tradePairMarketDataSnapshotRepository.GetAsync(m =>
            //     m.ChainId == recordInput.ChainId && m.TradePairId == recordInput.TradePairId && m.Timestamp == snapshotTime);
            // marketData.Volume.ShouldBe(700);
            // marketData.TradeValue.ShouldBe(1300);
            // marketData.TradeCount.ShouldBe(2);
            // marketData.TradeAddressCount24h.ShouldBe(1);
            
            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(2000);
            marketDataSnapshot.TradeValue.ShouldBe(4000);
            marketDataSnapshot.TradeCount.ShouldBe(2);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);
            
            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(2000);
            tradePair.TradeValue24h.ShouldBe(4000);
            tradePair.TradeCount24h.ShouldBe(2);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(100);
            
            var recordInput2 = new TradeRecordCreateDto()
            {
                ChainId = ChainId,
                Address = "0x",
                Side = TradeSide.Buy,
                Token0Amount = "100",
                Token1Amount = "200",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-3)),
                TransactionHash = "tx",
                TradePairId = TradePairEthUsdtId
            };
            await _tradeRecordAppService.CreateAsync(recordInput2);
            
            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(recordInput2.Timestamp));
            // marketData = await _tradePairMarketDataSnapshotRepository.GetAsync(m =>
            //     m.ChainId == recordInput2.ChainId && m.TradePairId == recordInput2.TradePairId && m.Timestamp == snapshotTime);
            // marketData.Volume.ShouldBe(700);
            // marketData.TradeValue.ShouldBe(1300);
            // marketData.TradeCount.ShouldBe(2);
            // marketData.TradeAddressCount24h.ShouldBe(1);
            
            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(100);
            marketDataSnapshot.TradeValue.ShouldBe(200);
            marketDataSnapshot.TradeCount.ShouldBe(1);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);
            
            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(2000);
            tradePair.TradeValue24h.ShouldBe(4000);
            tradePair.TradeCount24h.ShouldBe(2);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(100);
        }
    }
}