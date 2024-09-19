using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.StatInfo
{
    [Collection(ClusterCollection.Name)]
    public class StatInfoSnapshotHandlerTests : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
        private readonly IStatInfoAppService _statInfoAppService;
        private readonly DateTime _baseTime;
        
        public StatInfoSnapshotHandlerTests()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _statInfoSnapshotIndexRepository = GetRequiredService<INESTRepository<StatInfoSnapshotIndex, Guid>>();
            _statInfoAppService = GetRequiredService<IStatInfoAppService>();
            _baseTime = new DateTime(2024, 3, 2, 6, 5, 12);
        }

        [Fact]
        public async Task GetSnapshotTimestampTestAsync()
        {
            var time = new DateTime(2019, 3, 2, 3, 50, 12);
            var snapshotTime = StatInfoHelper.GetSnapshotTimestamp(3600 * 6, DateTimeHelper.ToUnixTimeMilliseconds(time));
            var snapDate = DateTimeHelper.FromUnixTimeMilliseconds(snapshotTime);
            snapDate.Day.ShouldBe(1);
            snapDate.Hour.ShouldBe(22);
        }

        [Fact]
        public async Task GetPoolPriceVolumeTest()
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(_baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 1,
                Timestamp = timestamp
            });

            var result = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            var hourSnapshotTime = new DateTime(_baseTime.Year, _baseTime.Month, _baseTime.Day, _baseTime.Hour, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(hourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);
            
            result = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            var sixHourSnapshotTime = new DateTime(_baseTime.Year, _baseTime.Month, _baseTime.Day, 4, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(sixHourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);
            
            var volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 15,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(_baseTime.AddHours(2))
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 2,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(_baseTime.AddHours(2))
            });
            
            result = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(2);
            result.Items[0].PriceInUsd.ShouldBe(10);
            result.Items[1].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.Items.Count.ShouldBe(2);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            volResult.Items[1].VolumeInUsd.ShouldBe(2);
            
            result = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(3);
        }
        
        [Fact]
        public async Task GetStatInfoTest()
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(_baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 0,
                Tvl = 11,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 0,
                VolumeInUsd = 22,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                PriceInUsd = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                Tvl = 13,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                VolumeInUsd = 1,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 20,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Tvl = 6,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 3,
                Timestamp = timestamp
            });
            
            // all
            var result = await _statInfoAppService.GetTvlListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].Tvl.ShouldBe(11);
            
            var volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(22);
            
            // token
            result = await _statInfoAppService.GetTvlListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].Tvl.ShouldBe(13);
            
            var priceResult = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            priceResult.Items.Count.ShouldBe(1);
            priceResult.Items[0].PriceInUsd.ShouldBe(10);
            
            volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            // pool
            result = await _statInfoAppService.GetTvlListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].Tvl.ShouldBe(6);
            
            priceResult = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            priceResult.Items.Count.ShouldBe(1);
            priceResult.Items[0].PriceInUsd.ShouldBe(20);
            
            volResult = await _statInfoAppService.GetVolumeListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(3);
            
        }
    }
}