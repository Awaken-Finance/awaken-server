using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradeRecordIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradeRecordEto>>,
        IDistributedEventHandler<EntityDeletedEto<TradeRecordEto>>

    {
        private readonly INESTRepository<TradeRecord, Guid> _tradeRecordIndexRepository;
        private readonly IPriceAppService _priceAppService;
        private readonly IAElfClientProvider _aelfClientProvider;
        private readonly ILogger<TradeRecordIndexHandler> _logger;
        private readonly IBus _bus;

        public TradeRecordIndexHandler(INESTRepository<TradeRecord, Guid> tradeRecordIndexRepository,
            IPriceAppService priceAppService,
            IAElfClientProvider aefClientProvider,
            IBus bus,
            ILogger<TradeRecordIndexHandler> logger)
        {
            _tradeRecordIndexRepository = tradeRecordIndexRepository;
            _priceAppService = priceAppService;
            _aelfClientProvider = aefClientProvider;
            _logger = logger;
            _bus = bus;
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradeRecordEto> eventData)
        {
            _logger.LogInformation(
                $"handle EntityCreatedEto<TradeRecordEto>");
            
            var index = ObjectMapper.Map<TradeRecordEto, TradeRecord>(eventData.Entity);
            index.TradePair = await GetTradePariWithTokenAsync(eventData.Entity.TradePairId);
            index.TotalPriceInUsd = await GetHistoryPriceInUsdAsync(index);
            index.TransactionFee =
                await _aelfClientProvider.GetTransactionFeeAsync(index.ChainId, index.TransactionHash) /
                Math.Pow(10, 8);

            _logger.LogInformation(
                $"handle EntityCreatedEto<TradeRecordEto> write es begin");
            await _tradeRecordIndexRepository.AddOrUpdateAsync(index);
            _logger.LogInformation(
                $"handle EntityCreatedEto<TradeRecordEto> write es end");
            
            await _bus.Publish(new NewIndexEvent<TradeRecordIndexDto>
            {
                Data = ObjectMapper.Map<TradeRecord, TradeRecordIndexDto>(index)
            });

            _logger.LogInformation(
                $"publish TradeRecordIndexDto address:{index.Address} tradePairId:{index.TradePair.Id} chainId:{index.ChainId} txId:{index.TransactionHash}");
        }

        public async Task HandleEventAsync(EntityDeletedEto<TradeRecordEto> eventData)
        {
        }

        private async Task<double> GetHistoryPriceInUsdAsync(TradeRecord index)
        {
            try
            {
                var list = await _priceAppService.GetTokenHistoryPriceDataAsync(
                    new List<GetTokenHistoryPriceInput>
                    {
                        new GetTokenHistoryPriceInput()
                        {
                            Symbol = index.TradePair.Token1.Symbol,
                            DateTime = index.Timestamp
                        }
                    });
                if (list.Items != null && list.Items.Count >= 1 &&
                    double.Parse(list.Items[0].PriceInUsd.ToString()) > 0)
                {
                    _logger.LogInformation("{token1Symbol}, time: {time}, get history price: {price}",
                        index.TradePair.Token1.Symbol, index.Timestamp, list.Items[0].PriceInUsd.ToString());
                    return index.Price * double.Parse(index.Token0Amount) *
                           double.Parse(list.Items[0].PriceInUsd.ToString());
                }

                if (index.TradePair.Token0.Symbol == "USDT")
                {
                    return double.Parse(index.Token0Amount);
                }

                if (index.TradePair.Token1.Symbol == "USDT")
                {
                    return double.Parse(index.Token1Amount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get history price failed.");
            }

            return 0;
        }
    }
}