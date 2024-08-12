using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using Volo.Abp.ObjectMapping;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Trade
{
    [RemoteService(IsEnabled = false)]
    public class LimitOrderAppService : ApplicationService, ILimitOrderAppService
    {
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<LimitOrderAppService> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITokenPriceProvider _tokenPriceProvider;

        public LimitOrderAppService(
            IGraphQLProvider graphQlProvider,
            IClusterClient clusterClient,
            ITokenAppService tokenAppService,
            ILogger<LimitOrderAppService> logger,
            IObjectMapper objectMapper,
            ITokenPriceProvider tokenPriceProvider)
        {
            _graphQlProvider = graphQlProvider;
            _clusterClient = clusterClient;
            _logger = logger;
            _objectMapper = objectMapper;
            _tokenAppService = tokenAppService;
            _tokenPriceProvider = tokenPriceProvider;
        }

        private async Task<Dictionary<string, TokenPriceDto>> GetTokens(List<LimitOrderDto> limitOrderDtos)
        {
            if (limitOrderDtos.Count <= 0)
            {
                return new Dictionary<string, TokenPriceDto>();
            }
            
            var tokenMap = new Dictionary<string, TokenPriceDto>();
            
            var symbolList = limitOrderDtos
                .SelectMany(i => new[] { i.SymbolIn, i.SymbolOut })
                .Distinct()
                .ToList();

            var chainId = limitOrderDtos[0].ChainId;
            
            foreach (var tokenSymbol in symbolList)
            {
                var tokenDetails = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = tokenSymbol
                });
                tokenMap[tokenSymbol] = new TokenPriceDto()
                {
                    Token = tokenDetails,
                    Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(chainId, tokenSymbol)
                };
            }

            return tokenMap;
        }
        
        public async Task<PagedResultDto<LimitOrderIndexDto>> GetListAsync(GetLimitOrdersInput input)
        {
            var queryResult = await _graphQlProvider.QueryLimitOrderAsync(input);
            var dataList = new List<LimitOrderIndexDto>();
            var tokenMap = await GetTokens(queryResult.Data);
            
            foreach (var limitOrder in queryResult.Data)
            {
                var limitOrderIndexDto = _objectMapper.Map<LimitOrderDto, LimitOrderIndexDto>(limitOrder);
                var token0 = tokenMap[limitOrderIndexDto.SymbolIn].Token;
                var token1 = tokenMap[limitOrderIndexDto.SymbolOut].Token;
                
                limitOrderIndexDto.MakerAddress = limitOrder.Maker;
                limitOrderIndexDto.TradePair = new TradePairWithTokenDto()
                {
                    ChainId = limitOrderIndexDto.ChainId,
                    Token0 = token0,
                    Token1 = token1
                };

                var token0Price = tokenMap[limitOrderIndexDto.SymbolIn].Price;
                var token1Price = tokenMap[limitOrderIndexDto.SymbolOut].Price;

                limitOrderIndexDto.AmountIn =
                    limitOrder.AmountIn.ToDecimalsString(tokenMap[limitOrderIndexDto.SymbolIn].Token.Decimals);
                limitOrderIndexDto.AmountInUSD = (Double.Parse(limitOrderIndexDto.AmountIn) * token0Price).ToString();

                limitOrderIndexDto.AmountOut =
                    limitOrder.AmountOut.ToDecimalsString(tokenMap[limitOrderIndexDto.SymbolOut].Token.Decimals);
                limitOrderIndexDto.AmountOutUSD = (Double.Parse(limitOrderIndexDto.AmountOut) * token1Price).ToString();

                limitOrderIndexDto.AmountInFilled =
                    limitOrder.AmountInFilled.ToDecimalsString(tokenMap[limitOrderIndexDto.SymbolIn].Token.Decimals);
                limitOrderIndexDto.AmountInFilledUSD =
                    (Double.Parse(limitOrderIndexDto.AmountInFilled) * token0Price).ToString();

                limitOrderIndexDto.AmountOutFilled =
                    limitOrder.AmountOutFilled.ToDecimalsString(tokenMap[limitOrderIndexDto.SymbolOut].Token.Decimals);
                limitOrderIndexDto.AmountOutFilledUSD =
                    (Double.Parse(limitOrderIndexDto.AmountOutFilled) * token1Price).ToString();

                var totalFee = 0d;
                var networkFee = double.Parse(limitOrder.TransactionFee.ToDecimalsString(8));
                
                foreach (var fillRecord in limitOrder.FillRecords)
                {
                    totalFee += double.Parse(fillRecord.TotalFee.ToDecimalsString(token0.Decimals));
                    networkFee += double.Parse(fillRecord.TransactionFee.ToDecimalsString(8));
                }

                limitOrderIndexDto.TotalFee = totalFee.ToString();
                limitOrderIndexDto.NetworkFee = networkFee.ToString();
                
                dataList.Add(limitOrderIndexDto);
            }

            return new PagedResultDto<LimitOrderIndexDto>()
            {
                TotalCount = queryResult.TotalCount,
                Items = dataList
            };
        }

        public async Task<PagedResultDto<LimitOrderFillRecordIndexDto>> GetListAsync(GetLimitOrderDetailsInput input)
        {
            var queryResult = await _graphQlProvider.QueryLimitOrderAsync(new GetLimitOrderDetailsInput
            {
                OrderId = input.OrderId
            });

            _logger.LogInformation($"Query limit order detail: {input.OrderId}, result count: {queryResult.TotalCount}");
            
            if (queryResult.Data == null || queryResult.Data.Count <= 0)
            {
                return new PagedResultDto<LimitOrderFillRecordIndexDto>();
            }
            
            var tokenMap = await GetTokens(queryResult.Data);

            var orderLimit = queryResult.Data[0];
            var totalFillRecordCount = orderLimit.FillRecords.Count;
            var pageData = orderLimit.FillRecords.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
            var resultData = new List<LimitOrderFillRecordIndexDto>();
            
            var token0Price = tokenMap[orderLimit.SymbolIn].Price;
            var token1Price = tokenMap[orderLimit.SymbolOut].Price;
            
            foreach (var fillRecord in pageData)
            {
                var fillRecordDto = _objectMapper.Map<FillRecord, LimitOrderFillRecordIndexDto>(fillRecord);
                
                fillRecordDto.AmountInFilled = fillRecord.AmountInFilled.ToDecimalsString(tokenMap[orderLimit.SymbolIn].Token.Decimals);
                fillRecordDto.AmountInFilledUSD = (Double.Parse(fillRecordDto.AmountInFilled) * token0Price).ToString();
                
                fillRecordDto.AmountOutFilled = fillRecord.AmountOutFilled.ToDecimalsString(tokenMap[orderLimit.SymbolOut].Token.Decimals);
                fillRecordDto.AmountOutFilledUSD = (Double.Parse(fillRecordDto.AmountOutFilled) * token1Price).ToString();
                
                fillRecordDto.NetworkFee = fillRecord.TransactionFee.ToDecimalsString(8);
                fillRecordDto.TotalFee = fillRecord.TotalFee.ToDecimalsString(tokenMap[orderLimit.SymbolIn].Token.Decimals);
                
                resultData.Add(fillRecordDto);
            }
            
            return new PagedResultDto<LimitOrderFillRecordIndexDto>()
            {
                TotalCount = totalFillRecordCount,
                Items = resultData
            };
        }

        public class TokenPriceDto
        {
            public TokenDto Token { get; set; }
            public double Price { get; set; }
        }
    }
}