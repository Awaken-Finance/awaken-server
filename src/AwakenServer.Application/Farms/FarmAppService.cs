using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Farms.Entities.Es;
using AwakenServer.Farms.Helpers;
using AwakenServer.Farms.Options;
using AwakenServer.Farms.Services;
using AwakenServer.Price.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    [RemoteService(IsEnabled = false)]
    public class FarmAppService : AwakenServerAppService, IFarmAppService
    {
        private readonly INESTReaderRepository<Farm, Guid> _farmReaderRepository;
        private readonly INESTReaderRepository<FarmPool, Guid> _farmPoolReaderRepository;
        private readonly INESTReaderRepository<FarmUserInfo, Guid> _farmUserInfoReaderRepository;
        private readonly INESTReaderRepository<FarmRecord, Guid> _farmRecordReaderRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IFarmAppPriceService _farmAppPriceService;
        private readonly ILogger<FarmAppService> _logger;
        private readonly string _mainToken;

        public FarmAppService(INESTReaderRepository<Farm, Guid> farmReaderRepository,
            INESTReaderRepository<FarmPool, Guid> farmPoolReaderRepository,
            INESTReaderRepository<FarmUserInfo, Guid> farmUserInfoReaderRepository,
            INESTReaderRepository<FarmRecord, Guid> farmRecordReaderRepository, IChainAppService chainAppService,
            IFarmAppPriceService farmAppPriceService, ILogger<FarmAppService> logger,
            IOptionsSnapshot<FarmOption> farmOptionsSnapshot)
        {
            _farmReaderRepository = farmReaderRepository;
            _farmPoolReaderRepository = farmPoolReaderRepository;
            _farmUserInfoReaderRepository = farmUserInfoReaderRepository;
            _farmRecordReaderRepository = farmRecordReaderRepository;
            _chainAppService = chainAppService;
            _farmAppPriceService = farmAppPriceService;
            _logger = logger;
            _mainToken = farmOptionsSnapshot.Value.MainToken;
        }

        public async Task<ListResultDto<FarmDto>> GetFarmListAsync(GetFarmInput input)
        {
            var farmList = (await _farmReaderRepository.GetListAsync(GetFarmFilterQueryContainer(input))).Item2;
            return new ListResultDto<FarmDto>
            {
                Items = ObjectMapper.Map<List<Farm>, List<FarmDto>>(farmList)
            };
        }

        public async Task<ListResultDto<FarmPoolDto>> GetFarmPoolListAsync(GetFarmPoolInput input)
        {
            List<Guid> poolIdList = null;
            if (!string.IsNullOrEmpty(input.User))
            {
                var userList = (await GetFarmUserInfoListAsync(new GetFarmUserInfoInput
                {
                    FarmId = input.FarmId,
                    ChainId = input.ChainId,
                    PoolId = input.PoolId,
                    User = input.User
                })).Items.ToList();
                poolIdList = userList.Select(x => x.PoolInfo.Id).ToList();
                if (!poolIdList.Any())
                {
                    return new ListResultDto<FarmPoolDto>();
                }
            }

            var poolList = (await _farmPoolReaderRepository.GetListAsync(
                GetFarmPoolFilterQueryContainer(input, poolIdList),
                limit: FarmConstants.DefaultSize)).Item2;
            var poolDtoList = ObjectMapper.Map<List<FarmPool>, List<FarmPoolDto>>(poolList);
            if (input.IsUpdateReward)
            {
                await UpdatePoolRevenueInformationAsync(poolDtoList);
            }

            return new ListResultDto<FarmPoolDto>
            {
                Items = poolDtoList
            };
        }

        public async Task<ListResultDto<FarmUserInfoDto>> GetFarmUserInfoListAsync(GetFarmUserInfoInput input)
        {
            var userInfos = (await _farmUserInfoReaderRepository.GetListAsync(
                GetUserFilterQueryContainer(input)
                , limit: FarmConstants.DefaultUserInfoSize)).Item2;
            var userDtoInfos = ObjectMapper.Map<List<FarmUserInfo>, List<FarmUserInfoDto>>(userInfos);

            if (!input.IsWithDetailPool)
            {
                return new ListResultDto<FarmUserInfoDto>
                {
                    Items = userDtoInfos
                };
            }

            var pools = (await GetFarmPoolListAsync(new GetFarmPoolInput
            {
                FarmId = input.FarmId,
                User = input.User,
                ChainId = input.ChainId,
                PoolId = input.PoolId,
                IsUpdateReward = input.IsWithApy
            })).Items;
            foreach (var userDto in userDtoInfos)
            {
                var pool = pools.FirstOrDefault(x => userDto.PoolInfo.Id == x.Id);
                if (pool != null)
                {
                    userDto.PoolDetailInfo = pool;
                }
            }

            return new ListResultDto<FarmUserInfoDto>
            {
                Items = userDtoInfos
            };
        }

        public async Task<PagedResultDto<FarmRecordDto>> GetFarmRecordListAsync(GetFarmRecordInput input)
        {
            var skipCount = input.SkipCount > 0 ? input.SkipCount : 0;
            var size = input.Size > 0 ? input.Size : FarmConstants.DefaultRecordSize;

            var totalCount = await _farmRecordReaderRepository.CountAsync(GetRecordFilterQueryContainer(input));
            var records = (await _farmRecordReaderRepository.GetListAsync(GetRecordFilterQueryContainer(input),
                null,
                x => x.Date,
                input.IsAscend ? SortOrder.Ascending : SortOrder.Descending,
                size,
                skipCount
            )).Item2;
            return new PagedResultDto<FarmRecordDto>
            {
                Items = ObjectMapper.Map<List<FarmRecord>, List<FarmRecordDto>>(records),
                TotalCount = totalCount.Count
            };
        }

        private Func<QueryContainerDescriptor<Farm>, QueryContainer> GetFarmFilterQueryContainer(
            GetFarmInput input)
        {
            return q =>
            {
                if (input.FarmId.HasValue)
                {
                    return q
                        .Term(t => t
                            .Field(f => f.Id).Value(input.FarmId.Value));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId));
                }

                return null;
            };
        }

        private Func<QueryContainerDescriptor<FarmPool>, QueryContainer> GetFarmPoolFilterQueryContainer(
            GetFarmPoolInput input, IReadOnlyCollection<Guid> poolIds = null)
        {
            return q =>
            {
                QueryContainer totalQueryContainer = null;
                if (poolIds != null && poolIds.Any())
                {
                    totalQueryContainer = q
                        .Terms(i => i
                            .Field(f => f.Id)
                            .Terms(poolIds));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId)) && totalQueryContainer;
                }

                if (input.FarmId.HasValue)
                {
                    return q
                        .Term(i => i
                            .Field(f => f.FarmId).Value(input.FarmId.Value)) && totalQueryContainer;
                }

                if (input.PoolId.HasValue)
                {
                    return q
                        .Term(i => i
                            .Field(f => f.Id).Value(input.PoolId.Value));
                }

                return totalQueryContainer;
            };
        }

        private Func<QueryContainerDescriptor<FarmUserInfo>, QueryContainer> GetUserFilterQueryContainer(
            GetFarmUserInfoInput input)
        {
            return q =>
            {
                QueryContainer totalQueryContainer = null;
                if (!string.IsNullOrEmpty(input.User))
                {
                    totalQueryContainer = q
                        .Term(i => i
                            .Field(f => f.User).Value(input.User));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId));
                }

                if (input.FarmId.HasValue)
                {
                    return totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.FarmInfo.Id).Value(input.FarmId.Value));
                }

                if (input.PoolId.HasValue)
                {
                    return totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.PoolInfo.Id).Value(input.PoolId.Value));
                }

                return totalQueryContainer;
            };
        }

        private async Task UpdatePoolRevenueInformationAsync
            (List<FarmPoolDto> poolDtoList)
        {
            var farmDic = new Dictionary<Guid, FarmDto>();
            var pendingDic = new Dictionary<string, (BigInteger, BigInteger)>();
            var chainsHeightDic = await GetChainsCurrentHeightAsync();
            var tokenPriceDic = new Dictionary<string, decimal>();
            var farmTokenPrices = await GetFarmTokenPriceAsync(poolDtoList);
            foreach (var pool in poolDtoList)
            {
                pool.PendingProjectToken = FarmConstants.ZeroBalance;
                pool.PendingUsdt = FarmConstants.ZeroBalance;
                if (pool.Weight == 0)
                {
                    continue;
                }
                var currentBlockHeight = chainsHeightDic[pool.ChainId];
                if (!farmDic.TryGetValue(pool.FarmId, out var farm))
                {
                    farm = (await GetFarmListAsync(new GetFarmInput
                    {
                        FarmId = pool.FarmId
                    })).Items[0];
                    farmDic.TryAdd(pool.FarmId, farm);
                }

                if (!tokenPriceDic.TryGetValue(pool.ChainId, out var tokenPrice))
                {
                    tokenPrice = await GetProjectTokenUsdtPriceAsync(pool.ChainId);
                    tokenPriceDic.TryAdd(pool.ChainId, tokenPrice);
                }

                BigInteger usdtPending;
                BigInteger tokenPending;
                if (!pendingDic.TryGetValue(farm.Id.ToString() + pool.LastUpdateBlockHeight, out var pending))
                {
                    (usdtPending, tokenPending) = GetPendingRevenue(farm, pool, currentBlockHeight);
                    pendingDic.TryAdd(farm.Id.ToString() + pool.LastUpdateBlockHeight, (usdtPending, tokenPending));
                }
                else
                {
                    usdtPending = pending.Item1;
                    tokenPending = pending.Item2;
                }
                var poolTokenKey = GetFarmPoolTokenKey(pool.ChainId, pool.SwapToken.Address, pool.SwapToken.Symbol);
                if (farmTokenPrices.TryGetValue(poolTokenKey, out var price))
                {
                    pool.SwapToken.TokenPrice = price;
                }
                else
                {
                    _logger.LogWarning(
                        $"Failed to get toke price; chain Id: {pool.ChainId}, token address: {pool.SwapToken.Address}, token symbol: {pool.SwapToken.Symbol}");
                }
                pool.PendingProjectToken = GetPoolPendingAmount(tokenPending, pool.Weight, farm.TotalWeight);
                pool.PendingUsdt = GetPoolPendingAmount(usdtPending, pool.Weight, farm.TotalWeight);
                
                
                pool.Apy1 = CalculatePoolProjectTokenApy(pool, farm, currentBlockHeight, tokenPrice);
                pool.Apy2 = CalculatePoolUsdtApy(pool, farm, currentBlockHeight, tokenPrice);
            }
        }

        private Func<QueryContainerDescriptor<FarmRecord>, QueryContainer> GetRecordFilterQueryContainer(
            GetFarmRecordInput input)
        {
            // return x =>
            //     (!input.ChainId.HasValue || x.PoolInfo.ChainId == input.ChainId.Value)
            //     && (!input.FarmId.HasValue || x.FarmInfo.Id == input.FarmId.Value)
            //     && (input.StartTime <= 0 || x.DateTime >= input.StartTime && x.DateTime <= input.EndTime)
            //     && input.User == x.User
            //     && (!input.BehaviorType.HasValue || input.BehaviorType.Value == x.BehaviorType)
            //     && (!input.TokenId.HasValue || input.TokenId.Value == x.TokenInfo.Id);

            return q =>
            {
                QueryContainer totalQueryContainer = q
                    .Term(t => t
                        .Field(f => f.User).Value(input.User));
                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.PoolInfo.ChainId).Value(input.ChainId));
                }

                if (input.FarmId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.FarmInfo.Id).Value(input.FarmId.Value));
                }

                if (input.StartTime > 0)
                {
                    var startTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.StartTime);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.Date)
                            .GreaterThanOrEquals(startTimeDate));
                }

                if (input.EndTime > 0)
                {
                    var endTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.EndTime);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.Date)
                            .LessThanOrEquals(endTimeDate));
                }

                if (input.BehaviorType.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.BehaviorType).Value(input.BehaviorType.Value));
                }

                if (input.TokenId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.TokenInfo.Id).Value(input.TokenId.Value));
                }

                return totalQueryContainer;
            };
        }

        private async Task<Dictionary<string, long>> GetChainsCurrentHeightAsync()
        {
            return (await _chainAppService.GetListAsync(new GetChainInput
            {
                IsNeedBlockHeight = true
            })).Items.ToDictionary(x => x.Id, x => x.LatestBlockHeight);
        }

        private string GetPoolPendingAmount(BigInteger totalPending, int poolWeight, int totalWeight)
        {
            if (poolWeight == 0 || totalWeight == 0)
            {
                return FarmConstants.ZeroBalance;
            }
            
            return (totalPending * poolWeight / totalWeight).ToString();
        }

        private decimal CalculatePoolProjectTokenApy(FarmPoolDto poolDto, FarmDto farmDto, long currentBlock, decimal tokenPrice)
        {
            var apyWithoutPrice = ProjectTokenCalculationHelper.CalculatePoolProjectTokenApyWithoutPrice(poolDto.Pid, poolDto.SwapToken.Decimals, poolDto.TotalDepositAmount,
                farmDto.FarmType, farmDto.StartBlock,
                farmDto.MiningHalvingPeriod1,
                farmDto.MiningHalvingPeriod2,
                BigInteger.Parse(farmDto.ProjectTokenMinePerBlock1), BigInteger.Parse(farmDto.ProjectTokenMinePerBlock2),
                farmDto.TotalWeight,
                poolDto.Weight, currentBlock);
            return decimal.Round(apyWithoutPrice * tokenPrice, 8);
        }

        private decimal CalculatePoolUsdtApy(FarmPoolDto poolDto, FarmDto farmDto, long currentBlock, decimal tokenPrice)
        {
            var apyWithoutPrice = ProjectTokenCalculationHelper.CalculatePoolUsdtApyWithoutPrice(poolDto.SwapToken.Decimals, farmDto.UsdtDividendPerBlock,
                farmDto.UsdtDividendStartBlockHeight, farmDto.UsdtDividendEndBlockHeight, poolDto.Weight,
                farmDto.TotalWeight, poolDto.TotalDepositAmount, currentBlock);
            return decimal.Round(apyWithoutPrice * tokenPrice, 8);
        }

        private async Task<Dictionary<string, decimal>> GetFarmTokenPriceAsync(List<FarmPoolDto> poolDtoList)
        {
            var ret = new Dictionary<string, decimal>();
            foreach (var chainFarmPoolInfo in poolDtoList.GroupBy(x => x.ChainId))
            {
                var chainId = chainFarmPoolInfo.Key;
                var farmTokenPrices = await _farmAppPriceService.GetSwapTokenPricesAsync(new GetSwapTokenPricesInput
                {
                    ChainId = chainId,
                    TokenAddresses = chainFarmPoolInfo.Select(t => t.SwapToken.Address).ToArray(),
                    TokenSymbol = chainFarmPoolInfo.Select(t => t.SwapToken.Symbol).ToArray()
                });

                foreach (var farmTokenPrice in farmTokenPrices.Where(farmTokenPrice => !ret.TryAdd(
                    GetFarmPoolTokenKey(chainId, farmTokenPrice.TokenAddress, farmTokenPrice.TokenSymbol),
                    decimal.Parse(farmTokenPrice.Price))))
                {
                    _logger.LogWarning(
                        $"Failed to add token price; chain Id: {chainId}, token address: {farmTokenPrice.TokenAddress}, token symbol: {farmTokenPrice.TokenSymbol}");
                }
            }

            return ret;
        }

        private string GetFarmPoolTokenKey(string chainId, string tokenAddress, string tokenSymbol)
        {
            return $"{chainId}{tokenAddress??string.Empty}{tokenSymbol??string.Empty}";
        }

        private (BigInteger usdtPending, BigInteger projectTokenPending) GetPendingRevenue(FarmDto farm, FarmPoolDto pool,
            long to, long from = 0)
        {
            from = from == 0 ? pool.LastUpdateBlockHeight : from;
            return ProjectTokenCalculationHelper.EstimatePendingRevenue(farm.FarmType, farm.StartBlock,
                farm.MiningHalvingPeriod1,
                farm.MiningHalvingPeriod2,
                BigInteger.Parse(farm.ProjectTokenMinePerBlock1), BigInteger.Parse(farm.ProjectTokenMinePerBlock2), farm.TotalWeight,
                farm.UsdtDividendStartBlockHeight,
                farm.UsdtDividendEndBlockHeight, BigInteger.Parse(farm.UsdtDividendPerBlock),
                from, pool.Weight, to);
        }
        
        private async Task<decimal> GetProjectTokenUsdtPriceAsync(string chainId)
        {
            var tokenPrice = await _farmAppPriceService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                ChainId = chainId,
                Symbol = _mainToken
            });
            return decimal.Parse(tokenPrice);
        }
    }
}