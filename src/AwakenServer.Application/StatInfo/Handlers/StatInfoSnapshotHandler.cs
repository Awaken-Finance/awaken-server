using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.StatInfo;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Price;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade;
using AwakenServer.Trade.Etos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.StatInfo.Handlers
{
    public class StatInfoSnapshotHandler : ILocalEventHandler<StatInfoSnapshotEto>, ITransientDependency
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly StatInfoOptions _statInfoOptions;
        private readonly ILogger _logger;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IPriceAppService _priceAppService;

        public IDistributedEventBus _distributedEventBus { get; set; }

        public StatInfoSnapshotHandler(IClusterClient clusterClient,
            IObjectMapper objectMapper,
            IOptionsSnapshot<StatInfoOptions> statInfoPeriodOptions,
            IDistributedEventBus distributedEventBus,
            ITradePairAppService tradePairAppService,
            IPriceAppService priceAppService)
        {
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _statInfoOptions = statInfoPeriodOptions.Value;
            _distributedEventBus = distributedEventBus;
            _logger = Log.ForContext<StatInfoSnapshotHandler>();
            _tradePairAppService = tradePairAppService;
            _priceAppService = priceAppService;
        }

        private string GetGrainId(int period, StatInfoSnapshotEto eventData, long snapshotTime)
        {
            switch (eventData.StatType)
            {
                //all
                case 0:
                {
                    return GrainIdHelper.GenerateGrainId(eventData.ChainId, eventData.StatType, period, snapshotTime, eventData.Version);
                }
                //token
                case 1:
                {
                    return GrainIdHelper.GenerateGrainId(eventData.ChainId, eventData.StatType, eventData.Symbol,
                        period, snapshotTime, eventData.Version);
                }
                //pool
                case 2:
                {
                    return GrainIdHelper.GenerateGrainId(eventData.ChainId, eventData.StatType, eventData.PairAddress,
                        period, snapshotTime, eventData.Version);
                }
            }

            return null;
        }

        public async Task HandleEventAsync(StatInfoSnapshotEto eventData)
        {
            foreach (var period in _statInfoOptions.Periods)
            {
                var periodTimestamp = StatInfoHelper.GetSnapshotTimestamp(period, eventData.Timestamp);
                var id = GetGrainId(period, eventData, periodTimestamp);
                var grain = _clusterClient.GetGrain<IStatInfoSnapshotGrain>(id);

                var priceInUsd = eventData.PriceInUsd;
                if (eventData.StatType == (int)StatType.Pool)
                {
                    var tradePair =
                        await _tradePairAppService.GetTradePairAsync(eventData.ChainId, eventData.PairAddress);
                    if (tradePair == null)
                    {
                        _logger.Error(
                            $"handle StatInfoSnapshotEto, chain: {eventData.ChainId}, get pair: {eventData.PairAddress} failed");
                        return;
                    }

                    var symbol1PriceInUsd = await _priceAppService.GetTokenPriceAsync(tradePair.Token1.Symbol);
                    priceInUsd = eventData.Price * symbol1PriceInUsd;
                }
                
                var statInfoSnapshotGrainDto = _objectMapper.Map<StatInfoSnapshotEto,StatInfoSnapshotGrainDto>(eventData);
                statInfoSnapshotGrainDto.PriceInUsd = priceInUsd;
                statInfoSnapshotGrainDto.Period = period;
                statInfoSnapshotGrainDto.Timestamp = periodTimestamp;

                var result = await grain.AddOrUpdateAsync(statInfoSnapshotGrainDto);
                _logger.Debug($"StatInfoSnapshotIndexEto: {JsonConvert.SerializeObject(result.Data)}, Grain Id: {id}");
                if (result.Success)
                {
                    await _distributedEventBus.PublishAsync(
                        _objectMapper.Map<StatInfoSnapshotGrainDto, StatInfoSnapshotIndexEto>(result.Data));
                }
            }
        }
    }
}