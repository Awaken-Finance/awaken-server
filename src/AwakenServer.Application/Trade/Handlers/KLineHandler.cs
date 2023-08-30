using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade.Etos;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade.Handlers
{
    public class KLineHandler : ILocalEventHandler<NewTradeRecordEvent>, ITransientDependency
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly KLinePeriodOptions _kLinePeriodOptions;
        public IDistributedEventBus DistributedEventBus { get; set; }
        
        public KLineHandler(IClusterClient clusterClient,
            IObjectMapper objectMapper,
            IOptionsSnapshot<KLinePeriodOptions> kLinePeriodOptions)
        {
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _kLinePeriodOptions = kLinePeriodOptions.Value;
        }

        public async Task HandleEventAsync(NewTradeRecordEvent eventData)
        {
            var timeStamp = DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp);

            foreach (var period in _kLinePeriodOptions.Periods)
            {
                var periodTimestamp = KLineHelper.GetKLineTimestamp(period, timeStamp);
                var token0Amount = double.Parse(eventData.Token0Amount);
                
                var id = GrainIdHelper.GenerateGrainId(eventData.ChainId, eventData.TradePairId, period);
                var grain = _clusterClient.GetGrain<IKLineGrain>(id);
                var kLineGrainResult = await grain.GetAsync();
                if (!kLineGrainResult.Success)
                {
                    var kLine = new KLineGrainDto
                    {
                        ChainId = eventData.ChainId,
                        TradePairId = eventData.TradePairId,
                        Open = eventData.Price,
                        Close = eventData.Price,
                        High = eventData.Price,
                        Low = eventData.Price,
                        Volume = token0Amount,
                        Period = period,
                        Timestamp = periodTimestamp
                    };
                    await grain.AddOrUpdateAsync(kLine);
                    await DistributedEventBus.PublishAsync(_objectMapper.Map<KLineGrainDto, KLineEto>(kLine));
                }
                else
                {
                    var kLine = kLineGrainResult.Data;
                    if (kLine.Timestamp == periodTimestamp)
                    {
                        kLine.Close = eventData.Price;
                        if (kLine.High < eventData.Price)
                        {
                            kLine.High = eventData.Price;
                        }
                        if (kLine.Low > eventData.Price)
                        {
                            kLine.Low = eventData.Price;
                        }
                        kLine.Volume += token0Amount;
                    }
                    else
                    {
                        kLine.Timestamp = periodTimestamp;
                        kLine.Open = eventData.Price;
                        kLine.Close = eventData.Price;
                        kLine.High = eventData.Price;
                        kLine.Low = eventData.Price;
                        kLine.Volume = token0Amount;
                    }
                    await grain.AddOrUpdateAsync(kLine);
                    await DistributedEventBus.PublishAsync(_objectMapper.Map<KLineGrainDto, KLineEto>(kLine));
                }
            }
        }
    }
}