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
        public IDistributedEventBus _distributedEventBus { get; set; }

        public KLineHandler(IClusterClient clusterClient,
            IObjectMapper objectMapper,
            IOptionsSnapshot<KLinePeriodOptions> kLinePeriodOptions,
            IDistributedEventBus distributedEventBus)
        {
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _kLinePeriodOptions = kLinePeriodOptions.Value;
            _distributedEventBus = distributedEventBus;
        }

        public async Task HandleEventAsync(NewTradeRecordEvent eventData)
        {
            if (eventData.IsRevert)
            {
                return;
            }

            var timeStamp = DateTimeHelper.ToUnixTimeMilliseconds(eventData.Timestamp);

            foreach (var period in _kLinePeriodOptions.Periods)
            {
                var periodTimestamp = KLineHelper.GetKLineTimestamp(period, timeStamp);
                var token0Amount = double.Parse(eventData.Token0Amount);

                var id = GrainIdHelper.GenerateGrainId(eventData.ChainId, eventData.TradePairId, period);
                var grain = _clusterClient.GetGrain<IKLineGrain>(id);
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
                var result = await grain.AddOrUpdateAsync(kLine);
                if (result.Success)
                {
                    await _distributedEventBus.PublishAsync(_objectMapper.Map<KLineGrainDto, KLineEto>(result.Data));
                }
            }
        }
    }
}