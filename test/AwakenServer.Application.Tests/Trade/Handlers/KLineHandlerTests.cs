using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Tests;
using Microsoft.Extensions.Options;
using Nest;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.Trade.Handlers
{
    [Collection(ClusterCollection.Name)]
    public class KLineHandlerTests : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly INESTRepository<Index.KLine, Guid> _kLineIndexRepository;
        private readonly KLinePeriodOptions _kLinePeriodOptions;

        public KLineHandlerTests()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _kLineIndexRepository = GetRequiredService<INESTRepository<Index.KLine, Guid>>();
            _kLinePeriodOptions = GetRequiredService<IOptionsSnapshot<KLinePeriodOptions>>().Value;
        }

        [Fact]
        public async Task HandleEventSameTimeTest()
        {
            var timestamp = DateTime.UtcNow;
            await KLineSameTimeTest(10, 100, timestamp, 10, 10, 10, 10, 100);
            await KLineSameTimeTest(12, 100, timestamp, 10, 12, 12, 10, 200);
            await KLineSameTimeTest(11, 100, timestamp, 10, 11, 12, 10, 300);
            await KLineSameTimeTest(9, 100, timestamp, 10, 9, 12, 9, 400);
        }
        
        private async Task KLineSameTimeTest(double price, double volume, DateTime timestamp, double expectedOpen,
            double expectedClose, double expectedHigh, double expectedLow, double expectedVolume)
        {
            var eventData = new NewTradeRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Price = price,
                Timestamp = timestamp,
                Token0Amount = volume.ToString(),
                Token1Amount = "200"
            };
            await _eventBus.PublishAsync(eventData);
            
            foreach (var period in _kLinePeriodOptions.Periods)
            {
                var timeStamp = DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp);
                long periodTimestamp;
                if (period == 3600 * 24 * 7)
                {
                    var offset = 4 * 3600 * 24 * 1000;
                    var offsetTime = timeStamp - offset;
                    periodTimestamp = offsetTime - offsetTime % (period * 1000) + offset;
                }
                else
                {
                    periodTimestamp = timeStamp - timeStamp % (period * 1000);
                }
                var mustQuery = new List<Func<QueryContainerDescriptor<Index.KLine>, QueryContainer>>();
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(eventData.ChainId)));
                mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)));
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Timestamp).Value(periodTimestamp)));
                
                QueryContainer Filter(QueryContainerDescriptor<Index.KLine> f) => f.Bool(b => b.Must(mustQuery));
            
                var kLine = await _kLineIndexRepository.GetAsync(Filter);
                kLine.Open.ShouldBe(expectedOpen);
                kLine.Close.ShouldBe(expectedClose);
                kLine.High.ShouldBe(expectedHigh);
                kLine.Low.ShouldBe(expectedLow);
                kLine.Volume.ShouldBe(expectedVolume);
                kLine.Timestamp.ShouldBe(periodTimestamp);

                var kLineIndex = await GetListAsync(eventData.ChainId, eventData.TradePairId, period);
                kLineIndex.Count.ShouldBe(1);
                kLineIndex.Last().Open.ShouldBe(expectedOpen);
                kLineIndex.Last().Close.ShouldBe(expectedClose);
                kLineIndex.Last().High.ShouldBe(expectedHigh);
                kLineIndex.Last().Low.ShouldBe(expectedLow);
                kLineIndex.Last().Volume.ShouldBe(expectedVolume);
                kLineIndex.Last().Timestamp.ShouldBe(periodTimestamp);
            }
        }
        
        [Theory(Skip = "no need")]
        [InlineData(60)]
        [InlineData(60 * 15)]
        [InlineData(60 * 30)]
        [InlineData(3600)]
        [InlineData(3600 * 4)]
        [InlineData(3600 * 24)]
        [InlineData(3600 * 24 * 7)]
        public async Task HandleEventTest(int period)
        {
            var time = new DateTime(2021, 10, 25, 0, 0, 0, DateTimeKind.Utc);
            var eventData = new NewTradeRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Price = 10,
                Timestamp = time,
                Token0Amount = "100",
                Token1Amount = "200"
            };
            await _eventBus.PublishAsync(eventData);

            var kLineIndex = await GetListAsync(eventData.ChainId, eventData.TradePairId, period);
            kLineIndex.Count.ShouldBe(1);
            kLineIndex.Last().Open.ShouldBe(10);
            kLineIndex.Last().Close.ShouldBe(10);
            kLineIndex.Last().High.ShouldBe(10);
            kLineIndex.Last().Low.ShouldBe(10);
            kLineIndex.Last().Volume.ShouldBe(100);
            kLineIndex.Last().Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(time));
            
            eventData.Timestamp = eventData.Timestamp.AddSeconds(period-1);
            eventData.Price = eventData.Price + 1;
            eventData.Token0Amount = "101";
            await _eventBus.PublishAsync(eventData);
            
            kLineIndex = await GetListAsync(eventData.ChainId, eventData.TradePairId, period);
            kLineIndex.Count.ShouldBe(1);
            kLineIndex.Last().Open.ShouldBe(10);
            kLineIndex.Last().Close.ShouldBe(11);
            kLineIndex.Last().High.ShouldBe(11);
            kLineIndex.Last().Low.ShouldBe(10);
            kLineIndex.Last().Volume.ShouldBe(201);
            kLineIndex.Last().Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(time));
            
            eventData.Timestamp = eventData.Timestamp.AddSeconds(1);
            eventData.Price = eventData.Price + 1;
            eventData.Token0Amount = "102";
            await _eventBus.PublishAsync(eventData);

            kLineIndex = await GetListAsync(eventData.ChainId, eventData.TradePairId, period);
            kLineIndex.Count.ShouldBe(2);
            kLineIndex.Last().Open.ShouldBe(12);
            kLineIndex.Last().Close.ShouldBe(12);
            kLineIndex.Last().High.ShouldBe(12);
            kLineIndex.Last().Low.ShouldBe(12);
            kLineIndex.Last().Volume.ShouldBe(102);
            kLineIndex.Last().Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(time.AddSeconds(period)));
        }

        private async Task<List<Index.KLine>> GetListAsync(string chainId, Guid pairId, int period)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.KLine>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(pairId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));

            QueryContainer Filter(QueryContainerDescriptor<Index.KLine> f) => f.Bool(b => b.Must(mustQuery));
            
            var list = await _kLineIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);
            return list.Item2;
        }
    }
}