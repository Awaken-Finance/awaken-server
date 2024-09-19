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
            _baseTime = DateTime.UtcNow;
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
        public async Task GetListAsyncTest()
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(_baseTime);
            var eventData = new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            };
            
            await _eventBus.PublishAsync(eventData);

            var result = await _statInfoAppService.GetPriceListAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(10);
            
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 15,
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
        }
    }
}