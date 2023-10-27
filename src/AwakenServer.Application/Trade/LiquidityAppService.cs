using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Provider;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

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

        private const string ASC = "asc";
        private const string ASCEND = "ascend";
        private const string ASSETUSD = "assetusd";

        public LiquidityAppService(
            ITokenPriceProvider tokenPriceProvider,
            ITradePairAppService tradePairAppService,
            IGraphQLProvider graphQlProvider,
            IChainAppService chainAppService,
            IClusterClient clusterClient,
            IAElfClientProvider aefClientProvider,
            ILocalEventBus localEventBus,
            ILogger<LiquidityAppService> logger)
        {
            _tokenPriceProvider = tokenPriceProvider;
            _tradePairAppService = tradePairAppService;
            _graphQlProvider = graphQlProvider;
            _chainAppService = chainAppService;
            _clusterClient = clusterClient;
            _aelfClientProvider = aefClientProvider;
            _localEventBus = localEventBus;
            _logger = logger;
        }

        public async Task<PagedResultDto<LiquidityRecordIndexDto>> GetRecordsAsync(GetLiquidityRecordsInput input)
        {
            var qlQueryInput = new GetLiquidityRecordIndexInput();
            ObjectMapper.Map(input, qlQueryInput);
            if (input.TradePairId.HasValue)
            {
                var tradePairIndexDto = await _tradePairAppService.GetAsync(input.TradePairId.Value);
                qlQueryInput.Pair = tradePairIndexDto?.Address;
            }

            var queryResult = await _graphQlProvider.QueryLiquidityRecordAsync(qlQueryInput);
            var dataList = new List<LiquidityRecordIndexDto>();
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
                (await _tradePairAppService.GetListAsync(input.ChainId, pairAddresses)).GroupBy(t => t.Address)
                .Select(g => g.First()).ToDictionary(t => t.Address,
                    t => t);
            foreach (var recordDto in queryResult.Data)
            {
                var indexDto = new LiquidityRecordIndexDto();
                ObjectMapper.Map(recordDto, indexDto);
                indexDto.ChainId = input.ChainId;
                indexDto.TradePair = pairs.GetValueOrDefault(recordDto.Pair, null);
                if (indexDto.TradePair == null)
                {
                    continue;
                }
                bool isReversed = indexDto.TradePair.Token0.Symbol == recordDto.Token1;
                if (isReversed)
                {
                    indexDto.Token0Amount = recordDto.Token1Amount.ToDecimalsString(indexDto.TradePair.Token1.Decimals);
                    indexDto.Token1Amount = recordDto.Token0Amount.ToDecimalsString(indexDto.TradePair.Token0.Decimals);
                }
                else
                {
                    indexDto.Token0Amount = recordDto.Token0Amount.ToDecimalsString(indexDto.TradePair.Token0.Decimals);
                    indexDto.Token1Amount = recordDto.Token1Amount.ToDecimalsString(indexDto.TradePair.Token1.Decimals);
                }
                indexDto.LpTokenAmount = recordDto.LpTokenAmount.ToDecimalsString(8);
                indexDto.TransactionFee =
                   await _aelfClientProvider.GetTransactionFeeAsync(qlQueryInput.ChainId, recordDto.TransactionHash) / Math.Pow(10, 8);
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

        public async Task<PagedResultDto<UserLiquidityIndexDto>> GetUserLiquidityAsync(GetUserLiquidityInput input)
        {
            var dataList = new List<UserLiquidityIndexDto>();
            
            var queryResult = await _graphQlProvider.QueryUserLiquidityAsync(input);
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
                (await _tradePairAppService.GetListAsync(input.ChainId, pairAddresses)).GroupBy(t => t.Address)
                .Select(g => g.First()).ToDictionary(t => t.Address,
                    t => t);
            foreach (var dto in queryResult.Data)
            {
                var indexDto = new UserLiquidityIndexDto();
                ObjectMapper.Map(dto, indexDto);
                var tradePairIndex = pairs.GetValueOrDefault(dto.Pair, null);
                if (tradePairIndex == null)
                {
                    continue;
                }
                indexDto.TradePair = tradePairIndex;
                indexDto.LpTokenAmount = dto.LpTokenAmount.ToDecimalsString(8);

                var prop = tradePairIndex.TotalSupply == null || tradePairIndex.TotalSupply == "0" ? 0 : dto.LpTokenAmount / double.Parse(tradePairIndex.TotalSupply);
                indexDto.Token0Amount = ((long)(prop * tradePairIndex.ValueLocked0)).ToDecimalsString(8);
                indexDto.Token1Amount = ((long)(prop * tradePairIndex.ValueLocked1)).ToDecimalsString(8);
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

        public async Task<UserAssetDto> GetUserAssetAsync(GetUserAssertInput input)
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
            
            var btcPrice = await _tokenPriceProvider.GetTokenUSDPriceAsync(input.ChainId, "BTC");
            return new UserAssetDto
            {
                AssetUSD = asset,
                AssetBTC = btcPrice == 0 ? 0 : asset / btcPrice
            };
        }

        public async Task CreateAsync(LiquidityRecordCreateDto input)
        {
        }

        public async Task CreateAsync(LiquidityRecordDto input)
        {
            var grain = _clusterClient.GetGrain<ILiquiditySyncGrain>(GrainIdHelper.GenerateGrainId(input.ChainId, input.BlockHeight));
            if (await grain.ExistTransactionHashAsync(input.TransactionHash))
            {
                _logger.LogInformation("liquidity event transactionHash existed:{transactionHash}", input.TransactionHash);
                return;
            }

            var chain = await _chainAppService.GetByNameCacheAsync(input.ChainId);
            var tradePair = await _tradePairAppService.GetTradePairAsync(input.ChainId, input.Pair);
            if (tradePair == null)
            {
                _logger.LogInformation("tradePair not existed,chainId:{chainId},address:{address}", input.ChainId, input.Pair);
                throw new Exception("tradePair not existed");
            }

            var liquidityEvent = new NewLiquidityRecordEvent
            {
                ChainId = chain.Id,
                LpTokenAmount = input.LpTokenAmount.ToDecimalsString(8),
                TradePairId = tradePair.Id,
                Timestamp = DateTime.UnixEpoch.AddMilliseconds(input.Timestamp)
            };
            await _localEventBus.PublishAsync(liquidityEvent);
            await grain.AddTransactionHashAsync(input.TransactionHash);
        }
    }
}