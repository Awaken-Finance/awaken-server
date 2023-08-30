using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade.Dtos;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Trade
{
    public class KLineAppServiceTests : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly IKLineAppService _kLineAppService;

        public KLineAppServiceTests()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _kLineAppService = GetRequiredService<IKLineAppService>();
        }

        [Fact]
        public async Task AddKlineTestAsync()
        {
            var id = Guid.NewGuid().ToString();
            var kLine = new KLineGrainDto
            {
                ChainId = ChainId,
                TradePairId = TradePairBtcEthId,
                Period = 3600 * 24,
                Open = 10,
                Close = 10,
                High = 10,
                Low = 10
            };
            var grain =Cluster.Client.GetGrain<IKLineGrain>(id); 
            var result = await grain.AddOrUpdateAsync(kLine);
            result.Success.ShouldBeTrue();
            
            var kLineResult = await grain.GetAsync();
            kLineResult.Success.ShouldBeTrue();
            kLineResult.Data.GrainId.ToString().ShouldBe<string>(id);
        }

        [Fact]
        public async Task GetListAsyncTest()
        {
            var eventData = new NewTradeRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Price = 10,
                Timestamp = DateTime.UtcNow.AddDays(-3),
                Token0Amount = "100",
                Token1Amount = "200"
            };
            
            var kLines = await _kLineAppService.GetListAsync(new GetKLinesInput
            {
                ChainId = eventData.ChainId,
                Period = 3600 * 24,
                TradePairId = eventData.TradePairId,
                TimestampMin = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-7)),
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            });
            kLines.Items.Count.ShouldBe(0);
            
            await _eventBus.PublishAsync(eventData);
            
            eventData.Price = 9;
            await _eventBus.PublishAsync(eventData);
            
            var input = new GetKLinesInput
            {
                ChainId = eventData.ChainId,
                Period = 3600 * 24,
                TradePairId = eventData.TradePairId,
                TimestampMin = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-7)),
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            };
            kLines = await _kLineAppService.GetListAsync(input);
  
            kLines.Items.Count.ShouldBe(2);
            kLines.Items[0].Open.ShouldBe(10);
            kLines.Items[0].Close.ShouldBe(9);
            kLines.Items[0].Low.ShouldBe(9);
            kLines.Items[0].High.ShouldBe(10);
            kLines.Items[0].Volume.ShouldBe(200);
            kLines.Items[0].Timestamp.ShouldBe(KLineHelper.GetKLineTimestamp(input.Period,
                DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp)));
            kLines.Items[0].Period.ShouldBe(input.Period);
            kLines.Items[0].TradePairId.ShouldBe(input.TradePairId);
            kLines.Items[0].ChainId.ShouldBe(input.ChainId);
            kLines.Items[1].Open.ShouldBe(9);
            kLines.Items[1].Close.ShouldBe(9);
            kLines.Items[1].Low.ShouldBe(9);
            kLines.Items[1].High.ShouldBe(9);
            kLines.Items[1].Volume.ShouldBe(0);
            kLines.Items[1].Timestamp.ShouldBe(KLineHelper.GetKLineTimestamp(input.Period, input.TimestampMax));
            kLines.Items[1].Period.ShouldBe(input.Period);
            kLines.Items[1].TradePairId.ShouldBe(input.TradePairId);
            kLines.Items[1].ChainId.ShouldBe(input.ChainId);
            
            var input2 = new GetKLinesInput
            {
                ChainId = eventData.ChainId,
                Period = 3600 * 24,
                TradePairId = eventData.TradePairId,
                TimestampMin = KLineHelper.GetKLineTimestamp(input.Period,
                    DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp)),
                TimestampMax = KLineHelper.GetKLineTimestamp(input.Period,
                    DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp))
            };
            kLines = await _kLineAppService.GetListAsync(input2);
            kLines.Items.Count.ShouldBe(1);
            kLines.Items[0].Open.ShouldBe(10);
            kLines.Items[0].Close.ShouldBe(9);
            kLines.Items[0].Low.ShouldBe(9);
            kLines.Items[0].High.ShouldBe(10);
            kLines.Items[0].Volume.ShouldBe(200);
            kLines.Items[0].Timestamp.ShouldBe(input2.TimestampMin);
            kLines.Items[0].Period.ShouldBe(input2.Period);
            kLines.Items[0].TradePairId.ShouldBe(input2.TradePairId);
            kLines.Items[0].ChainId.ShouldBe(input2.ChainId);
            
            var eventData2 = new NewTradeRecordEvent
            {
                ChainId = ChainId,
                Address = "0x1",
                TradePairId = TradePairBtcEthId,
                Price = 5,
                Timestamp = DateTime.UtcNow.AddDays(-8),
                Token0Amount = "50",
                Token1Amount = "100"
            };
            await _eventBus.PublishAsync(eventData2);
            
            var input3 = new GetKLinesInput
            {
                ChainId = eventData.ChainId,
                Period = 3600 * 24,
                TradePairId = eventData.TradePairId,
                TimestampMin = DateTimeHelper.ToUnixTimeMilliseconds(eventData2.Timestamp.AddDays(2)),
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            };
            kLines = await _kLineAppService.GetListAsync(input3);
            kLines.Items.Count.ShouldBe(3);
            kLines.Items[0].Open.ShouldBe(5);
            kLines.Items[0].Close.ShouldBe(5);
            kLines.Items[0].Low.ShouldBe(5);
            kLines.Items[0].High.ShouldBe(5);
            kLines.Items[0].Volume.ShouldBe(0);
            kLines.Items[0].Timestamp.ShouldBe(KLineHelper.GetKLineTimestamp(input3.Period, input3.TimestampMin));
            kLines.Items[0].Period.ShouldBe(input3.Period);
            kLines.Items[0].TradePairId.ShouldBe(input3.TradePairId);
            kLines.Items[0].ChainId.ShouldBe(input3.ChainId);
            
            kLines.Items[1].Open.ShouldBe(10);
            kLines.Items[1].Close.ShouldBe(9);
            kLines.Items[1].Low.ShouldBe(9);
            kLines.Items[1].High.ShouldBe(10);
            kLines.Items[1].Volume.ShouldBe(200);
            kLines.Items[1].Timestamp.ShouldBe(KLineHelper.GetKLineTimestamp(input.Period,
                DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp)));
            kLines.Items[1].Period.ShouldBe(input3.Period);
            kLines.Items[1].TradePairId.ShouldBe(input3.TradePairId);
            kLines.Items[1].ChainId.ShouldBe(input3.ChainId);
            
            kLines.Items[2].Open.ShouldBe(9);
            kLines.Items[2].Close.ShouldBe(9);
            kLines.Items[2].Low.ShouldBe(9);
            kLines.Items[2].High.ShouldBe(9);
            kLines.Items[2].Volume.ShouldBe(0);
            kLines.Items[2].Timestamp.ShouldBe(KLineHelper.GetKLineTimestamp(input3.Period, input3.TimestampMax));
            kLines.Items[2].Period.ShouldBe(input3.Period);
            kLines.Items[2].TradePairId.ShouldBe(input3.TradePairId);
            kLines.Items[2].ChainId.ShouldBe(input3.ChainId);

            await Assert.ThrowsAsync<AbpValidationException>(async () => await _kLineAppService.GetListAsync(
                new GetKLinesInput
                {
                    TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
                    TimestampMin = 0,
                    Period = 1
                }));
        }
    }
}