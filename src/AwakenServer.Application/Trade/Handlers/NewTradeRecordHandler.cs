using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Price;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade.Handlers
{
    public class NewTradeRecordHandler : ILocalEventHandler<NewTradeRecordEvent>, ITransientDependency
    {
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly IObjectMapper _objectMapper;
        private readonly IPriceAppService _priceAppService;
        private readonly ILogger<NewTradeRecordHandler> _logger;        
        public NewTradeRecordHandler(ITradePairMarketDataProvider tradePairMarketDataProvider,
            ITradeRecordAppService tradeRecordAppService, IClusterClient clusterClient,
            IObjectMapper objectMapper,
            IPriceAppService priceAppService,
            ILogger<NewTradeRecordHandler> logger)
        {
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _tradeRecordAppService = tradeRecordAppService;
            _objectMapper = objectMapper;
            _priceAppService = priceAppService;
            _logger = logger;
        }

        public async Task HandleEventAsync(NewTradeRecordEvent eventData)
        {
            var tradeAddressCount24h = await _tradeRecordAppService.GetUserTradeAddressCountAsync(eventData.ChainId, eventData.TradePairId,
                eventData.Timestamp, eventData.Timestamp);
            var dto = _objectMapper.Map<NewTradeRecordEvent, TradeRecordGrainDto>(eventData);
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(eventData.TradePairId, async grain =>
            {
                return await grain.UpdateTradeRecordAsync(dto, tradeAddressCount24h);
            });
            try
            {
                await _priceAppService.UpdateAffectedPriceMapAsync(eventData.ChainId, eventData.TradePairId,
                    eventData.Token0Amount, eventData.Token1Amount);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Update affected price map from swap failed. {e.Message}");
            }
        }
    }
}