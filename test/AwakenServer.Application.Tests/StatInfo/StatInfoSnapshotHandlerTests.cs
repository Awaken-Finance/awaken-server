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
using MongoDB.Bson.IO;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Validation;
using Xunit;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.StatInfo
{
    [Collection(ClusterCollection.Name)]
    public class StatInfoSnapshotHandlerTests : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
        private readonly IStatInfoAppService _statInfoAppService;
        protected const string DataVersion = "v1";
        
        public StatInfoSnapshotHandlerTests()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _statInfoSnapshotIndexRepository = GetRequiredService<INESTRepository<StatInfoSnapshotIndex, Guid>>();
            _statInfoAppService = GetRequiredService<IStatInfoAppService>();
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
        public async Task PeriodTest()
        {
            var baseTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 5, 12);
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 1,
                Timestamp = timestamp
            });

            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.Items.Count.ShouldBe(1);
            var hourSnapshotTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(hourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);

            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.TradePair.Token0.Symbol.ShouldBe("BTC");
            result.TradePair.Token1.Symbol.ShouldBe("ETH");
            result.Items.Count.ShouldBe(1);
            var sixHourSnapshotTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 4, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(sixHourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);
        }

        [Fact]
        public async Task GetPoolPriceVolumeTest()
        {
            var baseTime = DateTime.UtcNow;
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime.AddHours(-2))
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 1,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime.AddHours(-2))
            });
            
            var volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 15,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 2,
                Timestamp = timestamp
            });
            
            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(2);
            result.Items[0].PriceInUsd.ShouldBe(10);
            result.Items[1].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            volResult.TotalVolumeInUsd.ShouldBe(3);
            volResult.Items.Count.ShouldBe(2);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            volResult.Items[1].VolumeInUsd.ShouldBe(2);
            
            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
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
            var baseTime = DateTime.UtcNow;
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 0,
                Tvl = 11,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 0,
                VolumeInUsd = 22,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                Price = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                Tvl = 13,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                VolumeInUsd = 1,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 20,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Tvl = 6,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 3,
                Timestamp = timestamp
            });
            
            // all
            var result = await _statInfoAppService.GetTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].Tvl.ShouldBe(11);
            
            var volResult = await _statInfoAppService.GetVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
            });
            volResult.TotalVolumeInUsd = 22;
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(22);
            
            // token
            var tokenResult = await _statInfoAppService.GetTokenTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            tokenResult.Items.Count.ShouldBe(1);
            tokenResult.Items[0].Tvl.ShouldBe(13);
            
            var priceResult = await _statInfoAppService.GetTokenPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            priceResult.Items.Count.ShouldBe(1);
            priceResult.Items[0].PriceInUsd.ShouldBe(10);
            
            var tokenVolResult = await _statInfoAppService.GetTokenVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            tokenVolResult.Items.Count.ShouldBe(1);
            tokenVolResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            // pool
            var poolResult = await _statInfoAppService.GetPoolTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolResult.Items.Count.ShouldBe(1);
            poolResult.Items[0].Tvl.ShouldBe(6);
            
            var poolPriceResult = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolPriceResult.Items.Count.ShouldBe(1);
            poolPriceResult.Items[0].PriceInUsd.ShouldBe(20);
            
            var poolVolResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolVolResult.Items.Count.ShouldBe(1);
            poolVolResult.Items[0].VolumeInUsd.ShouldBe(3);
            
        }
        
        [Fact]
        public async Task DataVersionTest()
        {
            var baseTime = DateTime.UtcNow;
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = "v2",
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            });

            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task CalculateApr7dTest()
        {
            var timestamp1 = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-8));
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 10,
                Tvl = 100000,
                Timestamp = timestamp1
            });

            var result = await _statInfoAppService.CalculateApr7dAsync(TradePairBtcEthAddress);
            result.ShouldBe(0.0);
            
            var timestamp2 = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1));
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 5,
                Tvl = 100000,
                Timestamp = timestamp2
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 3,
                Tvl = 100000,
                Timestamp = timestamp2
            });

            result = await _statInfoAppService.CalculateApr7dAsync(TradePairBtcEthAddress);
            result.ShouldBe(3.4680000000000004);
        }
    }
}