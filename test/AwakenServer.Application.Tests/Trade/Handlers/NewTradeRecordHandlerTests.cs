using System;
using System.Collections.Generic;
using System.Threading;
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
            for (int i = 0; i < 10; i++)
            {
                recordInput.TransactionHash = $"tx{i}";
                await _tradeRecordAppService.CreateAsync(recordInput);
            }


            var snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));

            var marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(10000);
            marketDataSnapshot.TradeValue.ShouldBe(20000);
            marketDataSnapshot.TradeCount.ShouldBe(10);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);

            var tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(10000);
            tradePair.TradeValue24h.ShouldBe(20000);
            tradePair.TradeCount24h.ShouldBe(10);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(0);


            for (int i = 10; i < 20; i++)
            {
                recordInput.Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddHours(-1));
                recordInput.Token0Amount = "1000";
                recordInput.Token1Amount = "2000";
                recordInput.TransactionHash = $"tx{i}";
                await _tradeRecordAppService.CreateAsync(recordInput);
            }


            await _tradeRecordAppService.CreateAsync(recordInput);
            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));


            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(10000);
            marketDataSnapshot.TradeValue.ShouldBe(20000);
            marketDataSnapshot.TradeCount.ShouldBe(10);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);

            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(10000);
            tradePair.TradeValue24h.ShouldBe(20000);
            tradePair.TradeCount24h.ShouldBe(10);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(0);


            for (int i = 20; i < 30; i++)
            {
                recordInput.Token0Amount = "1000";
                recordInput.Token1Amount = "2000";
                recordInput.TransactionHash = $"tx{i}";
                await _tradeRecordAppService.CreateAsync(recordInput);
            }


            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(recordInput.Timestamp));

            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(20000);
            marketDataSnapshot.TradeValue.ShouldBe(40000);
            marketDataSnapshot.TradeCount.ShouldBe(20);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);

            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(20000);
            tradePair.TradeValue24h.ShouldBe(40000);
            tradePair.TradeCount24h.ShouldBe(20);
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
            for (int i = 30; i < 40; i++)
            {
                recordInput2.TransactionHash = $"tx{i}";
                await _tradeRecordAppService.CreateAsync(recordInput2);
            }


            snapshotTime =
                _tradePairMarketDataProvider.GetSnapshotTime(
                    DateTimeHelper.FromUnixTimeMilliseconds(recordInput2.Timestamp));


            marketDataSnapshot = await _snapshotIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(recordInput2.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(recordInput2.TradePairId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
            marketDataSnapshot.Volume.ShouldBe(1000);
            marketDataSnapshot.TradeValue.ShouldBe(2000);
            marketDataSnapshot.TradeCount.ShouldBe(10);
            marketDataSnapshot.TradeAddressCount24h.ShouldBe(1);

            tradePair = await _tradePairIndexRepository.GetAsync(recordInput.TradePairId);
            tradePair.Volume24h.ShouldBe(20000);
            tradePair.TradeValue24h.ShouldBe(40000);
            tradePair.TradeCount24h.ShouldBe(20);
            tradePair.TradeAddressCount24h.ShouldBe(1);
            tradePair.VolumePercentChange24h.ShouldBe(100);
        }
    }
}