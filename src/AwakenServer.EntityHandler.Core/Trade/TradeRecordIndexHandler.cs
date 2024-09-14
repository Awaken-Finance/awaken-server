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
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradeRecordIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradeRecordEto>>,
        IDistributedEventHandler<EntityCreatedEto<MultiTradeRecordEto>>,
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
            index.TradePair = await GetTradePariWithTokenAsync(eventData.Entity.TradePairId);
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
                $"publish normal swap record, " +
                $"address:{index.Address} " +
                $"tradePairId:{index.TradePair.Id} " +
                $"chainId:{index.ChainId} " +
                $"txId:{index.TransactionHash}");
        }
        
        public async Task HandleEventAsync(EntityCreatedEto<MultiTradeRecordEto> eventData)
        {
            if (eventData.Entity.PercentRoutes.Count <= 0)
            {
                _logger.LogError($"creare multi swap records handle entity create event faild. percent routes empty.");
                return;
            }
            
            var index = ObjectMapper.Map<MultiTradeRecordEto, TradeRecord>(eventData.Entity);
            index.TradePair = await GetTradePariWithSwapRecordsAsync(eventData.Entity.PercentRoutes[0].Route);
            index.TotalPriceInUsd = await GetHistoryPriceInUsdAsync(index);
            index.TransactionFee =
                await _aelfClientProvider.GetTransactionFeeAsync(index.ChainId, index.TransactionHash) /
                Math.Pow(10, 8);
            
            _logger.LogInformation($"creare multi swap records handle entity create event. " +
                                   $"record: {JsonConvert.SerializeObject(index)}");
            
            await _tradeRecordIndexRepository.AddOrUpdateAsync(index);
            
            foreach (var record in eventData.Entity.SwapRecords)
            {
                if (record.IsLimitOrder)
                {
                    continue;
                }
                var pair = await GetTradePariWithTokenAsync(record.TradePairId);
                var isSell = pair.Token0.Symbol == record.SymbolIn;
                
                var subRecordIndex = ObjectMapper.Map<MultiTradeRecordEto, TradeRecord>(eventData.Entity);
                subRecordIndex.IsSubRecord = true;
                subRecordIndex.TradePair = pair;
                subRecordIndex.TransactionFee = index.TransactionFee;
                subRecordIndex.Side = isSell ? TradeSide.Sell : TradeSide.Buy;
                subRecordIndex.Token0Amount = isSell
                    ? record.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : record.AmountOut.ToDecimalsString(pair.Token0.Decimals);
                subRecordIndex.Token1Amount = isSell
                    ? record.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : record.AmountIn.ToDecimalsString(pair.Token1.Decimals);
                subRecordIndex.Price = double.Parse(subRecordIndex.Token1Amount) / double.Parse(subRecordIndex.Token0Amount);
                subRecordIndex.Id = Guid.NewGuid();
                subRecordIndex.TotalFee =
                    record.TotalFee / Math.Pow(10, isSell ? pair.Token0.Decimals : pair.Token1.Decimals);
                
                subRecordIndex.TotalPriceInUsd = await GetHistoryPriceInUsdAsync(subRecordIndex);
                
                await _tradeRecordIndexRepository.AddOrUpdateAsync(subRecordIndex);
                
                _logger.LogInformation($"creare multi swap records handle entity create event. " +
                                       $"record: {JsonConvert.SerializeObject(subRecordIndex)}");
                
                await _bus.Publish(new NewIndexEvent<TradeRecordIndexDto>
                {
                    Data = ObjectMapper.Map<TradeRecord, TradeRecordIndexDto>(subRecordIndex)
                });
            }
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get history price failed.");
            }

            return 0;
        }
    }
}