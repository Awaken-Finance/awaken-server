using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using SwapRecord = AwakenServer.Trade.SwapRecord;
using TradeRecord = AwakenServer.Trade.Index.TradeRecord;

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
            var index = ObjectMapper.Map<TradeRecordEto, TradeRecord>(eventData.Entity);
            index.TradePair = eventData.Entity.Side == TradeSide.Swap ? await GetTradePariWithSwapRecordsAsync(eventData.Entity.SwapRecords)
                : await GetTradePariWithTokenAsync(eventData.Entity.TradePairId);
            index.TotalPriceInUsd = await GetHistoryPriceInUsdAsync(index);
            index.TransactionFee =
                await _aelfClientProvider.GetTransactionFeeAsync(index.ChainId, index.TransactionHash) /
                Math.Pow(10, 8);
            
            await _tradeRecordIndexRepository.AddOrUpdateAsync(index);
            
            await _bus.Publish(new NewIndexEvent<TradeRecordIndexDto>
            {
                Data = ObjectMapper.Map<TradeRecord, TradeRecordIndexDto>(index)
            });

            _logger.LogInformation(
                $"publish TradeRecordIndexDto address:{index.Address} tradePairId:{index.TradePair.Id} chainId:{index.ChainId} txId:{index.TransactionHash}");
        }
        
        public async Task HandleEventAsync(EntityCreatedEto<TradeRecordPathEto> eventData)
        {
            // publish first pair
            var index = ObjectMapper.Map<TradeRecordPathEto, TradeRecord>(eventData.Entity);
            index.TradePair = await GetTradePariWithTokenAsync(eventData.Entity.TradePairId);
            index.TotalPriceInUsd = await GetHistoryPriceInUsdAsync(index);
            index.TransactionFee =
                await _aelfClientProvider.GetTransactionFeeAsync(index.ChainId, index.TransactionHash) /
                Math.Pow(10, 8);
            await _bus.Publish(new NewIndexEvent<TradeRecordIndexDto>
            {
                Data = ObjectMapper.Map<TradeRecord, TradeRecordIndexDto>(index)
            });

            // publish other pairs
            foreach (var record in eventData.Entity.SwapRecords)
            {
                
            }
            
            // fake pair info
            
            await _tradeRecordIndexRepository.AddOrUpdateAsync(index);
            
        }

        public async Task HandleEventAsync(EntityDeletedEto<TradeRecordEto> eventData)
        {
        }
        
        protected async Task<TradePairWithToken> GetTradePariWithSwapRecordsAsync(List<SwapRecord> swapRecords)
        {
            var firstTradePair = await GetTradePariWithTokenAsync(swapRecords[0].TradePairId); 
            var pairWithToken = new TradePairWithToken();
            var token0 = await TokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = swapRecords[0].SymbolIn
            });
            var count = swapRecords.Count;
            var token1 = await TokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = swapRecords[count - 1].SymbolOut
            });
            pairWithToken.Token0 = ObjectMapper.Map<TokenDto, Token>(token0);
            pairWithToken.Token1 = ObjectMapper.Map<TokenDto, Token>(token1);
            pairWithToken.FeeRate = firstTradePair.FeeRate;
            pairWithToken.ChainId = firstTradePair.ChainId;
            pairWithToken.Address = firstTradePair.Address;
            return pairWithToken;
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