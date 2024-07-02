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
    public class LiquidityAppService : ApplicationService, ILiquidityAppService
    {
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly IChainAppService _chainAppService;
        private readonly IClusterClient _clusterClient;
        private readonly IAElfClientProvider _aelfClientProvider;
        private readonly ILocalEventBus _localEventBus;
        private readonly ILogger<LiquidityAppService> _logger;
        private readonly IRevertProvider _revertProvider;
        private readonly IObjectMapper _objectMapper;
        private readonly IAElfClientProvider _blockchainClientProvider;
        private readonly ContractsTokenOptions _contractsTokenOptions;
        
        private const string ASC = "asc";
        private const string ASCEND = "ascend";
        private const string ASSETUSD = "assetusd";
        private const string BTCSymbol = "BTC";

        public LiquidityAppService(
            ITokenPriceProvider tokenPriceProvider,
            ITradePairAppService tradePairAppService,
            IGraphQLProvider graphQlProvider,
            IChainAppService chainAppService,
            IClusterClient clusterClient,
            IAElfClientProvider aefClientProvider,
            ILocalEventBus localEventBus,
            ILogger<LiquidityAppService> logger,
            IRevertProvider revertProvider,
            IObjectMapper objectMapper,
            IAElfClientProvider blockchainClientProvider,
            IOptions<ContractsTokenOptions> contractsTokenOptions)
        {
            _tokenPriceProvider = tokenPriceProvider;
            _tradePairAppService = tradePairAppService;
            _graphQlProvider = graphQlProvider;
            _chainAppService = chainAppService;
            _clusterClient = clusterClient;
            _aelfClientProvider = aefClientProvider;
            _localEventBus = localEventBus;
            _logger = logger;
            _revertProvider = revertProvider;
            _objectMapper = objectMapper;
            _blockchainClientProvider = blockchainClientProvider;
            _contractsTokenOptions = contractsTokenOptions.Value;
        }

        
        public async Task<PagedResultDto<LiquidityRecordIndexDto>> GetRecordsAsync(GetLiquidityRecordsInput input)
        {
            _logger.LogInformation($"GetRecordsAsync, {JsonConvert.SerializeObject(input)}");
            
            var qlQueryInput = new GetLiquidityRecordIndexInput();

            try
            {
                ObjectMapper.Map(input, qlQueryInput);
                if (input.TradePairId.HasValue)
                {
                    var tradePairIndexDto = await _tradePairAppService.GetFromGrainAsync(input.TradePairId.Value);
                    qlQueryInput.Pair = tradePairIndexDto?.Address;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get tradePairIndexDto failed.");
                throw;
            }
            
            var queryResult = await _graphQlProvider.QueryLiquidityRecordAsync(qlQueryInput);
            var dataList = new List<LiquidityRecordIndexDto>();
            
            _logger.LogInformation($"QueryLiquidityRecordAsync data count: {queryResult.Data.Count}");
            
            if (queryResult.TotalCount == 0 || queryResult.Data.IsNullOrEmpty())
            {
                return new PagedResultDto<LiquidityRecordIndexDto>
                {
                    Items = dataList,
                    TotalCount = queryResult.TotalCount
                };
            }

            var pairAddresses = queryResult.Data.Select(i => i.Pair).Distinct().ToList();
            var pairs =
                (await _tradePairAppService.GetListFromEsAsync(input.ChainId, pairAddresses)).GroupBy(t => t.Address)
                .Select(g => g.First()).ToDictionary(t => t.Address,
                    t => t);
            
            
            _logger.LogInformation($"get trade pairs: {JsonConvert.SerializeObject(pairs)}");
            foreach (var recordDto in queryResult.Data)
            {
                var indexDto = new LiquidityRecordIndexDto();
                ObjectMapper.Map(recordDto, indexDto);
                indexDto.ChainId = input.ChainId;
                indexDto.TradePair = pairs.GetValueOrDefault(recordDto.Pair, null);
                if (indexDto.TradePair == null)
                {
                    _logger.LogError($"can't find trade pair {recordDto.Pair}");
                    continue;
                }

                var isReversed = indexDto.TradePair.Token0.Symbol == recordDto.Token1;
                if (isReversed)
                {
                    indexDto.Token0Amount = recordDto.Token1Amount.ToDecimalsString(indexDto.TradePair.Token0.Decimals);
                    indexDto.Token1Amount = recordDto.Token0Amount.ToDecimalsString(indexDto.TradePair.Token1.Decimals);
                }
                else
                {
                    indexDto.Token0Amount = recordDto.Token0Amount.ToDecimalsString(indexDto.TradePair.Token0.Decimals);
                    indexDto.Token1Amount = recordDto.Token1Amount.ToDecimalsString(indexDto.TradePair.Token1.Decimals);
                }

                indexDto.LpTokenAmount = recordDto.LpTokenAmount.ToDecimalsString(8);
                indexDto.TransactionFee =
                    await _aelfClientProvider.GetTransactionFeeAsync(qlQueryInput.ChainId, recordDto.TransactionHash) /
                    Math.Pow(10, 8);
                _logger.LogInformation($"liquidity record index, txn hash :{indexDto.TransactionHash}, Txn fee{indexDto.TransactionFee}");
                dataList.Add(indexDto);
            }

            return new PagedResultDto<LiquidityRecordIndexDto>
            {
                TotalCount = queryResult.TotalCount,
                Items = dataList
            };
        }

        private List<UserLiquidityIndexDto> SortingUserLiquidity(string sorting, List<UserLiquidityIndexDto> dataList)
        {
            var result = dataList;
            if (string.IsNullOrWhiteSpace(sorting)) return result;

            var sortingArray = sorting.Trim().ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            switch (sortingArray.Length)
            {
                case 1:
                    switch (sortingArray[0])
                    {
                        case ASSETUSD:
                            result = dataList.OrderBy(o => o.AssetUSD).ToList();
                            break;
                    }

                    break;
                case 2:
                    switch (sortingArray[0])
                    {
                        case ASSETUSD:
                            result = sortingArray[1] == ASC || sortingArray[1] == ASCEND
                                ? dataList.OrderBy(o => o.AssetUSD).ToList()
                                : dataList.OrderBy(o => o.AssetUSD).Reverse().ToList();
                            break;
                    }

                    break;
            }

            return result;
        }
        
        public async Task<PagedResultDto<UserLiquidityIndexDto>> GetUserLiquidityFromGraphQLAsync(GetUserLiquidityInput input)
        {
            var dataList = new List<UserLiquidityIndexDto>();

            var queryResult = await _graphQlProvider.QueryUserLiquidityAsync(input);
            _logger.LogInformation($"GetUserLiquidityFromGraphQLAsync data count: {queryResult.Data.Count}");
            
            if (queryResult.TotalCount == 0 || queryResult.Data.IsNullOrEmpty())
            {
                return new PagedResultDto<UserLiquidityIndexDto>
                {
                    Items = dataList,
                    TotalCount = queryResult.TotalCount
                };
            }

            var pairAddresses = queryResult.Data.Select(i => i.Pair).Distinct().ToList();
            var pairs =
                (await _tradePairAppService.GetListFromEsAsync(input.ChainId, pairAddresses)).GroupBy(t => t.Address)
                .Select(g => g.First()).ToDictionary(t => t.Address,
                    t => t);
            foreach (var dto in queryResult.Data)
            {
                var indexDto = new UserLiquidityIndexDto();
                ObjectMapper.Map(dto, indexDto);
                var tradePairIndex = pairs.GetValueOrDefault(dto.Pair, null);
                if (tradePairIndex == null)
                {
                    _logger.LogError($"can't find trade pair {dto.Pair}");
                    continue;
                }

                indexDto.TradePair = tradePairIndex;
                indexDto.LpTokenAmount = dto.LpTokenAmount.ToDecimalsString(8);
                
                // var token = await GetTokenInfoAsync(tradePairIndex.Id, tradePairIndex.ChainId);
                // var supply = token != null ? token.Supply.ToDecimalsString(token.Decimals) : "0";
                // tradePairIndex.TotalSupply = supply;
                
                var prop = tradePairIndex.TotalSupply == null || tradePairIndex.TotalSupply == "0"
                    ? 0
                    : dto.LpTokenAmount / double.Parse(tradePairIndex.TotalSupply);
                
                
                _logger.LogInformation(
                    "User liquidity, tradePairIndex.TotalSupply: {supply}, " +
                    "dto.LpTokenAmount: {LpTokenAmount}, " +
                    "prop:{prop}, " +
                    "token0:{token0} " +
                    "decimal:{token0Decimal}," +
                    "token1:{token1} " +
                    "decimal:{token1Decimal}," +
                    "token0 amount:{amount0}," +
                    "token1 amount:{amoun1}",
                    tradePairIndex.TotalSupply, 
                    dto.LpTokenAmount, 
                    prop, 
                    tradePairIndex.Token0.Symbol, 
                    tradePairIndex.Token0.Decimals,
                    tradePairIndex.Token1.Symbol, 
                    tradePairIndex.Token1.Decimals, 
                    tradePairIndex.ValueLocked0,
                    tradePairIndex.ValueLocked1);
                
                

                indexDto.Token0Amount = tradePairIndex.Token0.Decimals == 0
                    ? Math.Floor(prop / Math.Pow(10, 8) * tradePairIndex.ValueLocked0).ToString()
                    : ((long)(prop * tradePairIndex.ValueLocked0)).ToDecimalsString(8);
                indexDto.Token1Amount = tradePairIndex.Token1.Decimals == 0
                    ? Math.Floor(prop / Math.Pow(10, 8) * tradePairIndex.ValueLocked1).ToString()
                    : ((long)(prop * tradePairIndex.ValueLocked1)).ToDecimalsString(8);

                indexDto.AssetUSD = tradePairIndex.TVL * prop / Math.Pow(10, 8);
                dataList.Add(indexDto);
            }

            dataList = SortingUserLiquidity(input.Sorting, dataList);
            return new PagedResultDto<UserLiquidityIndexDto>
            {
                Items = dataList,
                TotalCount = queryResult.TotalCount
            };
        }

        
        public async Task<UserAssetDto> GetUserAssetFromGraphQLAsync(GetUserAssertInput input)
        {
            var getUserLiquidityInput = new GetUserLiquidityInput();
            ObjectMapper.Map(input, getUserLiquidityInput);
            var queryResult = await _graphQlProvider.QueryUserLiquidityAsync(getUserLiquidityInput);
            if (queryResult.TotalCount == 0)
            {
                return new UserAssetDto();
            }

            var pairAddresses = queryResult.Data.Select(i => i.Pair).Distinct().ToList();
            var pairs =
                (await _tradePairAppService.GetListAsync(input.ChainId, pairAddresses)).GroupBy(t => t.Address)
                .Select(g => g.First()).ToDictionary(t => t.Address,
                    t => t);

            double asset = 0;
            foreach (var liquidity in queryResult.Data)
            {
                var pair = pairs.GetValueOrDefault(liquidity.Pair, null);
                if (pair == null || pair.TotalSupply == null || pair.TotalSupply == "0")
                {
                    continue;
                }

                var totalSupply = double.Parse(pair.TotalSupply);
                asset += pair.TVL * double.Parse(liquidity.LpTokenAmount.ToDecimalsString(8)) / totalSupply;
            }

            var btcPrice = await _tokenPriceProvider.GetTokenUSDPriceAsync(input.ChainId, BTCSymbol);
            return new UserAssetDto
            {
                AssetUSD = asset,
                AssetBTC = btcPrice == 0 ? 0 : asset / btcPrice
            };
        }

        
        public async Task<bool> UpdateTradePairFieldAsync(LiquidityRecordDto input)
        {
            var tradePair = await _tradePairAppService.GetTradePairAsync(input.ChainId, input.Pair);
            if (tradePair == null)
            {
                _logger.LogInformation("tradePair not existed,chainId:{chainId}, address:{address}", input.ChainId,
                    input.Pair);
                return false;
            }
            
            var liquidityEvent = new NewLiquidityRecordEvent
            {
                ChainId = input.ChainId,
                LpTokenAmount = input.LpTokenAmount.ToDecimalsString(8),
                TradePairId = tradePair.Id,
                Timestamp = DateTimeHelper.FromUnixTimeMilliseconds(input.Timestamp),
                Type = input.Type,
                IsRevert = input.IsRevert
            };
            await _localEventBus.PublishAsync(liquidityEvent);
            return true;
        }

        public async Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords)
        {
            if (needDeletedTradeRecords.IsNullOrEmpty())
            {
                return;
            }
                
            var needRevertDatas = new List<LiquidityRecordDto>();
            foreach (var transactionHash in needDeletedTradeRecords)
            {
                var grain = _clusterClient.GetGrain<ILiquidityRecordGrain>(GrainIdHelper.GenerateGrainId(chainId, transactionHash));
                var result = await grain.GetAsync();
                if (result.Success)
                {
                    needRevertDatas.Add(_objectMapper.Map<LiquidityRecordGrainDto, LiquidityRecordDto>(result.Data));
                }
            }
                
            foreach (var revertLiquidity in needRevertDatas)
            {
                revertLiquidity.IsRevert = true;
                await UpdateTradePairFieldAsync(revertLiquidity);
            }
        }
        
        public async Task RevertLiquidityAsync(string chainId)
        {
            try
            {
                var needDeletedTradeRecords =
                    await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.LiquidityEvent, chainId);

                await DoRevertAsync(chainId, needDeletedTradeRecords);
            }
            catch (Exception e)
            {
                _logger.LogError("Revert liquidity err:{0}", e);
            }
        }
        
        public async Task<bool> CreateAsync(long currentConfirmedHeight, LiquidityRecordDto input)
        {
            var grain = _clusterClient.GetGrain<ILiquidityRecordGrain>(
                GrainIdHelper.GenerateGrainId(input.ChainId, input.TransactionHash));
            if (await grain.ExistAsync())
            {
                return true;
            }

            await _revertProvider.CheckOrAddUnconfirmedTransaction(currentConfirmedHeight, EventType.LiquidityEvent, input.ChainId, input.BlockHeight, input.TransactionHash);
            
            var succeed = await UpdateTradePairFieldAsync(input);

            if (succeed)
            {
                await grain.AddAsync(_objectMapper.Map<LiquidityRecordDto, LiquidityRecordGrainDto>(input));
            }

            return succeed;
        }
    }
}