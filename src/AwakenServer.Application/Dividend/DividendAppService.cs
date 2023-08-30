using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Dividend.DividendAppDto;
using AwakenServer.Dividend.Entities.Es;
using AwakenServer.Dividend.Options;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Dividend
{
    [RemoteService(IsEnabled = false)]
    public class DividendAppService : AwakenServerAppService, IDividendAppService
    {
        private readonly INESTReaderRepository<Entities.Dividend, Guid> _dividendReaderRepository;
        private readonly INESTReaderRepository<DividendPool, Guid> _dividendPoolReaderRepository;
        private readonly INESTReaderRepository<DividendPoolToken, Guid> _poolTokenReaderRepository;
        private readonly INESTReaderRepository<DividendToken, Guid> _dividendTokenReaderRepository;
        private readonly INESTReaderRepository<DividendUserPool, Guid> _userPoolReaderRepository;
        private readonly INESTReaderRepository<DividendUserToken, Guid> _userTokenReaderRepository;
        private readonly INESTReaderRepository<DividendUserRecord, Guid> _userRecordReaderRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;
        private readonly ILogger<DividendAppService> _logger;
        private readonly long _blocksPerDay;

        public DividendAppService(INESTReaderRepository<Entities.Dividend, Guid> dividendReaderRepository,
            INESTReaderRepository<DividendPool, Guid> dividendPoolReaderRepository,
            INESTReaderRepository<DividendPoolToken, Guid> poolTokenReaderRepository,
            INESTReaderRepository<DividendToken, Guid> dividendTokenReaderRepository,
            INESTReaderRepository<DividendUserPool, Guid> userPoolReaderRepository,
            INESTReaderRepository<DividendUserToken, Guid> userTokenReaderRepository,
            INESTReaderRepository<DividendUserRecord, Guid> userRecordReaderRepository,
            IChainAppService chainAppService, ILogger<DividendAppService> logger,
            ITokenPriceProvider tokenPriceProvider, ITokenAppService tokenAppService,
            IOptionsSnapshot<DividendOption> option)
        {
            _dividendReaderRepository = dividendReaderRepository;
            _dividendPoolReaderRepository = dividendPoolReaderRepository;
            _poolTokenReaderRepository = poolTokenReaderRepository;
            _dividendTokenReaderRepository = dividendTokenReaderRepository;
            _userPoolReaderRepository = userPoolReaderRepository;
            _userTokenReaderRepository = userTokenReaderRepository;
            _userRecordReaderRepository = userRecordReaderRepository;
            _chainAppService = chainAppService;
            _logger = logger;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenAppService = tokenAppService;
            _blocksPerDay = option.Value.BlocksPerDay;
        }

        public async Task<ListResultDto<DividendDto>> GetDividendAsync(GetDividendInput input)
        {
            var dividendList = (await _dividendReaderRepository.GetListAsync(GetDividendFilterQueryContainer(input)))
                .Item2;
            var dividendDtoList = ObjectMapper.Map<List<Entities.Dividend>, List<DividendDto>>(dividendList);
            var dividendToken =
                (await _dividendTokenReaderRepository.GetListAsync(GetDividendTokenFilterQueryContainer(input))).Item2;
            foreach (var dividendTokens in dividendToken.GroupBy(t => t.Dividend.Id))
            {
                var dividendDto = dividendDtoList.FirstOrDefault(x => x.Id == dividendTokens.Key);
                if (dividendDto == null)
                {
                    continue;
                }

                dividendDto.DividendTokens =
                    ObjectMapper.Map<List<DividendToken>, List<DividendTokenDto>>(dividendTokens.ToList());
            }

            return new ListResultDto<DividendDto>
            {
                Items = dividendDtoList
            };
        }

        public async Task<ListResultDto<DividendPoolDto>> GetDividendPoolsAsync(GetDividendPoolsInput input)
        {
            if (!input.DividendId.HasValue && !input.PoolId.HasValue)
            {
                return new ListResultDto<DividendPoolDto>();
            }

            var dividendPoolList = (await _dividendPoolReaderRepository.GetListAsync(
                    GetDividendPoolFilterQueryContainer(input)))
                .Item2;

            if (!dividendPoolList.Any())
            {
                return new ListResultDto<DividendPoolDto>();
            }

            var dividendPoolDtoList =
                ObjectMapper.Map<List<DividendPool>, List<DividendPoolDto>>(dividendPoolList);
            var dividendId = input.DividendId ?? dividendPoolDtoList.First().Dividend.Id;
            var chainId = dividendPoolDtoList.First().Dividend.ChainId;
            var poolTokenList = (await _poolTokenReaderRepository.GetListAsync(
                    GetPoolTokenFilterQueryContainer(dividendId, input.PoolId)))
                .Item2;
            foreach (var dividendPoolTokens in poolTokenList.GroupBy(x => x.PoolBaseInfo.Id))
            {
                var pool = dividendPoolDtoList.FirstOrDefault(x => x.Id == dividendPoolTokens.Key);
                if (pool == null)
                {
                    continue;
                }

                pool.DividendTokenInfo =
                    ObjectMapper.Map<List<DividendPoolToken>, List<DividendPoolTokenDto>>(dividendPoolTokens.ToList());
            }

            await CalculateExtraPoolInfoAsync(dividendPoolDtoList, chainId, dividendId);
            return new ListResultDto<DividendPoolDto>
            {
                Items = dividendPoolDtoList
            };
        }

        public async Task<DividendUserInformationDto> GetUserDividendAsync(GetUserDividendInput input)
        {
            var userPoolList = (await _userPoolReaderRepository.GetListAsync(GetUserPoolFilterQueryContainer(input)))
                .Item2;
            var userTokenList = (await _userTokenReaderRepository.GetListAsync(GetUserTokenFilterQueryContainer(input)))
                .Item2;
            return new DividendUserInformationDto
            {
                User = input.User,
                UserPools = ObjectMapper.Map<List<DividendUserPool>, List<DividendUserPoolDto>>(userPoolList),
                UserTokens = ObjectMapper.Map<List<DividendUserToken>, List<DividendUserTokenDto>>(userTokenList)
            };
        }

        public async Task<PagedResultDto<DividendUserRecordDto>> GetDividendUserRecordsAsync(
            GetDividendUserRecordsInput input)
        {
            var (skipCount, size) = GetPageCountInfo(input.SkipCount, input.Size);
            var totalCountInfo =
                await _userRecordReaderRepository.CountAsync(
                    GetUserRecordFilterQueryContainer(input));
            var userPublicOfferings = (await _userRecordReaderRepository.GetListAsync(
                    GetUserRecordFilterQueryContainer(input), null, x => x.DateTime, SortOrder.Descending,
                    size,
                    skipCount))
                .Item2;
            return new PagedResultDto<DividendUserRecordDto>
            {
                TotalCount = totalCountInfo.Count,
                Items = ObjectMapper.Map<List<DividendUserRecord>, List<DividendUserRecordDto>>(userPublicOfferings)
            };
        }

        public async Task<DividendStatisticDto> GetDividendPoolStatisticAsync(GetDividendPoolStatisticInput input)
        {
            var dividend = (await GetDividendAsync(new GetDividendInput
            {
                DividendId = input.DividendId
            })).Items.First();
            var chainId = dividend.ChainId;
            var chains =
                (await _chainAppService.GetListAsync(new GetChainInput
                {
                    IsNeedBlockHeight = true
                })).Items.ToDictionary(x => x.Id, x => x.LatestBlockHeight);
            var currentBlockHeight = chains[chainId];
            var dividendTokens = (await _dividendTokenReaderRepository.GetListAsync()).Item2.ToDictionary(
                x => x.Token.Id, x => x);
            var allPoolTokens = (await _poolTokenReaderRepository.GetListAsync()).Item2;
            var allPoolsDic =
                (await _dividendPoolReaderRepository.GetListAsync())
                .Item2.ToDictionary(x => x.Id, x => x);
            var totalAccumulativeValue = await GetTotalAccumulativeValueAsync(allPoolTokens,
                dividendTokens, allPoolsDic, currentBlockHeight, dividend.TotalWeight);
            var totalDepositValue = await GetTotalDepositValueAsync(allPoolsDic.Values);
            var totalCurrentDividendValue =
                await GetTotalCurrentDividendValueAsync(chainId, dividendTokens, currentBlockHeight);
            return new DividendStatisticDto
            {
                TotalAccumulativeValue = totalAccumulativeValue.ToString("F8"),
                TotalDepositValue = totalDepositValue.ToString("F8"),
                TotalCurrentDividendValue = totalCurrentDividendValue.ToString("F8")
            };
        }

        public async Task<DividendUserStatisticDto> GetUserStatisticAsync(GetUserStatisticInput input)
        {
            var dividend = (await GetDividendAsync(new GetDividendInput
            {
                DividendId = input.DividendId
            })).Items.First();

            var userInfoDto = await GetUserDividendAsync(new GetUserDividendInput
            {
                DividendId = input.DividendId,
                User = input.User
            });
            double totalAccumulativeValue = 0;
            double totalDepositValue = 0;

            foreach (var userPool in userInfoDto.UserPools)
            {
                totalDepositValue += double.Parse(userPool.DepositAmount) /
                                     Math.Pow(10, userPool.PoolBaseInfo.PoolToken.Decimals) *
                                     await GetTokenUsdtPrice(dividend.ChainId,
                                         userPool.PoolBaseInfo.PoolToken.Symbol);
            }

            foreach (var dividendUserToken in userInfoDto.UserTokens)
            {
                totalAccumulativeValue += double.Parse(dividendUserToken.AccumulativeDividend) /
                    Math.Pow(10, dividendUserToken.DividendToken.Decimals) * await GetTokenUsdtPrice(dividend.ChainId,
                        dividendUserToken.DividendToken.Symbol);
            }

            return new DividendUserStatisticDto
            {
                TotalAccumulativeValue = totalAccumulativeValue.ToString("F8"),
                TotalDepositValue = totalDepositValue.ToString("F8")
            };
        }

        private Func<QueryContainerDescriptor<Entities.Dividend>, QueryContainer> GetDividendFilterQueryContainer(
            GetDividendInput input)
        {
            return q =>
            {
                if (input.DividendId.HasValue)
                {
                    return q
                        .Term(t => t
                            .Field(f => f.Id).Value(input.DividendId.Value));
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

        private Func<QueryContainerDescriptor<DividendToken>, QueryContainer> GetDividendTokenFilterQueryContainer(
            GetDividendInput input, long currentBlockHeight = 0)
        {
            return q =>
            {
                var blockQuery = q
                    .Range(r => r
                        .Field(f => f.EndBlock)
                        .GreaterThanOrEquals(currentBlockHeight)) && +q
                    .Range(r => r
                        .Field(f => f.StartBlock)
                        .LessThanOrEquals(currentBlockHeight));

                if (input.DividendId.HasValue)
                {
                    var dividendQuery = q
                        .Term(t => t
                            .Field(f => f.Dividend.Id).Value(input.DividendId.Value));
                    if (currentBlockHeight > 0)
                    {
                        return dividendQuery && +blockQuery;
                    }

                    return dividendQuery;
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    var chainQuery = q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId));
                    if (currentBlockHeight > 0)
                    {
                        return chainQuery && +blockQuery;
                    }

                    return chainQuery;
                }

                return currentBlockHeight > 0 ? blockQuery : null;
            };
        }

        private Func<QueryContainerDescriptor<DividendPool>, QueryContainer> GetDividendPoolFilterQueryContainer(
            GetDividendPoolsInput input)
        {
            return q =>
            {
                if (input.PoolId.HasValue)
                {
                    return q
                        .Term(t => t
                            .Field(f => f.Id).Value(input.PoolId.Value));
                }

                if (input.DividendId.HasValue)
                {
                    return q
                        .Term(i => i
                            .Field(f => f.Dividend.Id).Value(input.DividendId.Value));
                }

                return null;
            };
        }

        private Func<QueryContainerDescriptor<DividendPool>, QueryContainer> GetCurrentDividendPoolFilterQueryContainer(
            List<Guid> poolIdList)
        {
            return q => { return q.Terms(i => i.Field(f => f.Id).Terms(poolIdList)); };
        }

        private Func<QueryContainerDescriptor<DividendPoolToken>, QueryContainer> GetPoolTokenFilterQueryContainer(
            Guid dividendId, Guid? poolId)
        {
            return q =>
            {
                if (poolId.HasValue)
                {
                    return q
                        .Term(t => t
                            .Field(f => f.PoolBaseInfo.Id).Value(poolId.Value));
                }

                return q
                    .Term(i => i
                        .Field(f => f.PoolBaseInfo.Dividend.Id).Value(dividendId));
            };
        }

        private Func<QueryContainerDescriptor<DividendPoolToken>, QueryContainer>
            GetCurrentPoolTokenFilterQueryContainer(
                Guid dividendId, List<Guid> tokenIdList)
        {
            return q =>
            {
                return q
                           .Term(i => i
                               .Field(f => f.PoolBaseInfo.Dividend.Id).Value(dividendId)) &&
                       q.Terms(i => i.Field(f => f.DividendToken.Id).Terms(tokenIdList));
            };
        }

        private Func<QueryContainerDescriptor<DividendUserPool>, QueryContainer> GetUserPoolFilterQueryContainer(
            GetUserDividendInput input)
        {
            return q =>
            {
                return q
                           .Term(t => t
                               .Field(f => f.User).Value(input.User)) &&
                       q.Term(t => t
                           .Field(f => f.PoolBaseInfo.Dividend.Id).Value(input.DividendId));
            };
        }

        private Func<QueryContainerDescriptor<DividendUserToken>, QueryContainer> GetUserTokenFilterQueryContainer(
            GetUserDividendInput input)
        {
            return q =>
            {
                return q
                           .Term(t => t
                               .Field(f => f.User).Value(input.User)) &&
                       q.Term(t => t
                           .Field(f => f.PoolBaseInfo.Dividend.Id).Value(input.DividendId));
            };
        }

        private Func<QueryContainerDescriptor<DividendUserRecord>, QueryContainer>
            GetUserRecordFilterQueryContainer(
                GetDividendUserRecordsInput input)
        {
            return q =>
            {
                var totalQueryContainer = q
                                              .Term(t => t
                                                  .Field(f => f.PoolBaseInfo.Dividend.Id).Value(input.DividendId)) &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.User).Value(input.User));

                if (input.TimestampMax > 0)
                {
                    var endTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.DateTime)
                            .LessThanOrEquals(endTimeDate));
                }

                if (input.TimestampMin > 0)
                {
                    var startTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.DateTime)
                            .GreaterThanOrEquals(startTimeDate));
                }

                if (input.TokenId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.DividendToken.Id)
                                                  .Value(input.TokenId.Value));
                }

                if (input.PoolId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.PoolBaseInfo.Id)
                                                  .Value(input.PoolId.Value));
                }

                if (input.BehaviorType > 0)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.BehaviorType)
                                                  .Value(input.BehaviorType));
                }

                return totalQueryContainer;
            };
        }

        private async Task CalculateExtraPoolInfoAsync(List<DividendPoolDto> poolDtoList, string chainId, Guid dividendId)
        {
            var dividend = await GetDividendAsync(new GetDividendInput
            {
                DividendId = dividendId
            });
            var totalWeight = dividend.Items.First().TotalWeight;
            var chains =
                (await _chainAppService.GetListAsync(new GetChainInput
                {
                    IsNeedBlockHeight = true
                })).Items.ToDictionary(x => x.Id, x => x.LatestBlockHeight);
            if (!chains.TryGetValue(chainId, out var currentBlockHeight))
            {
                _logger.LogWarning($"Chain: {chainId} current block height not found");
                return;
            }

            var dividendTokenInfos = await GetDividendTokensByHeightAsync(dividendId, currentBlockHeight);

            foreach (var pool in poolDtoList)
            {
                pool.Apy = await CalculatePoolApyAsync(pool, currentBlockHeight);
                if (pool.Weight == 0 || !dividendTokenInfos.Any())
                {
                    continue;
                }

                CalculateToDistributedDividend(dividendTokenInfos, pool, totalWeight, currentBlockHeight);
            }
        }

        private async Task<List<DividendToken>> GetDividendTokensByHeightAsync(
            Guid dividendId, long currentBlockHeight)
        {
            var dividendToken = (await _dividendTokenReaderRepository.GetListAsync(
                GetDividendTokenFilterQueryContainer(new GetDividendInput
                {
                    DividendId = dividendId
                }, currentBlockHeight))).Item2;
            return dividendToken;
        }

        private void CalculateToDistributedDividend(List<DividendToken> dividendTokens, DividendPoolDto poolDto,
            int totalWeight, long currentBlockHeight)
        {
            poolDto.DividendTokenInfo ??= new List<DividendPoolTokenDto>();
            foreach (var dividendToken in dividendTokens)
            {
                var poolToken =
                    poolDto.DividendTokenInfo.SingleOrDefault(x => x.DividendToken.Id == dividendToken.Token.Id);
                if (poolToken == null)
                {
                    poolToken = new DividendPoolTokenDto
                    {
                        DividendToken = ObjectMapper.Map<Tokens.Token, TokenDto>(dividendToken.Token),
                        AccumulativeDividend = "0",
                        LastRewardBlock = 0
                    };
                    poolDto.DividendTokenInfo.Add(poolToken);
                }

                poolToken.ToDistributedDivided = "0";
                var startBlock = poolToken.LastRewardBlock > dividendToken.StartBlock
                    ? poolToken.LastRewardBlock
                    : dividendToken.StartBlock;
                var endBlock = currentBlockHeight > dividendToken.EndBlock
                    ? dividendToken.EndBlock
                    : currentBlockHeight;
                if (startBlock > endBlock)
                {
                    continue;
                }

                poolToken.ToDistributedDivided =
                    (BigInteger.Parse(dividendToken.AmountPerBlock) * GetBlockSpan(endBlock, startBlock) *
                     poolDto.Weight /
                     totalWeight).ToString();
            }
        }

        private (int, int) GetPageCountInfo(int skipCount, int size)
        {
            return (skipCount > 0 ? skipCount : 0,
                size > 0 ? size : 50);
        }

        private async Task<double> CalculatePoolApyAsync(DividendPoolDto dividendPoolDto, long currentBlockHeight)
        {
            return 1; // todo
        }

        private async Task<double> GetTotalAccumulativeValueAsync(List<DividendPoolToken> poolTokens,
            Dictionary<Guid, DividendToken> dividendTokenDic, Dictionary<Guid, DividendPool> poolDic,
            long currentBlockHeight, int totalWeight)
        {
            double totalAccumulativeValue = 0;
            foreach (var poolToken in poolTokens)
            {
                var accumulativeDividend = double.Parse(poolToken.AccumulativeDividend);
                if (poolDic.TryGetValue(poolToken.PoolBaseInfo.Id, out var targetPool) &&
                    dividendTokenDic.TryGetValue(poolToken.DividendToken.Id, out var dividendToken))
                {
                    var poolWeight = targetPool.Weight;
                    if (poolWeight != 0)
                    {
                        var endBlock = currentBlockHeight > dividendToken.EndBlock
                            ? dividendToken.EndBlock
                            : currentBlockHeight;
                        var startBlock = poolToken.LastRewardBlock > dividendToken.StartBlock
                            ? poolToken.LastRewardBlock
                            : dividendToken.StartBlock;
                        var blockDiff = endBlock > startBlock
                            ? GetBlockSpan(endBlock, startBlock)
                            : 0;
                        if (blockDiff != 0)
                        {
                            accumulativeDividend += blockDiff *
                                double.Parse(dividendToken.AmountPerBlock) * poolWeight / totalWeight;
                        }
                    }
                }

                totalAccumulativeValue += accumulativeDividend /
                                          Math.Pow(10, poolToken.DividendToken.Decimals) *
                                          await GetTokenUsdtPrice(poolToken.ChainId,
                                              poolToken.DividendToken.Symbol);
            }

            return totalAccumulativeValue;
        }

        private async Task<double> GetTotalDepositValueAsync(IEnumerable<DividendPool> pools)
        {
            double totalDepositValue = 0;
            foreach (var p in pools)
            {
                totalDepositValue += double.Parse(p.DepositAmount) /
                    Math.Pow(10, p.PoolToken.Decimals) * await GetTokenUsdtPrice(p.ChainId, p.PoolToken.Symbol);
            }

            return totalDepositValue;
        }

        private async Task<double> GetTotalCurrentDividendValueAsync(string chainId,
            Dictionary<Guid, DividendToken> dividendTokenDic, long currentBlockHeight)
        {
            double totalCurrentDividendValue = 0;
            foreach (var dividendToken in dividendTokenDic.Values)
            {
                if (currentBlockHeight < dividendToken.StartBlock || currentBlockHeight > dividendToken.EndBlock)
                {
                    continue;
                }

                var timeSpan = GetBlockSpan(dividendToken.EndBlock, dividendToken.StartBlock);
                var tokenPerBlock = double.Parse(dividendToken.AmountPerBlock) /
                                    Math.Pow(10, dividendToken.Token.Decimals);
                totalCurrentDividendValue += tokenPerBlock * timeSpan *
                                             await GetTokenUsdtPrice(chainId, dividendToken.Token.Symbol);
            }

            return totalCurrentDividendValue;
        }

        private async Task<double> GetTokenUsdtPrice(string chainId, string symbol)
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = symbol
            });

            if (token != null)
            {
                return await _tokenPriceProvider.GetTokenUSDPriceAsync(chainId, symbol);
            }

            _logger.LogWarning($"Lack of token {symbol}-usdt price");
            return 0;
        }

        private long GetBlockSpan(long endBlock, long startBlock)
        {
            return endBlock - startBlock;
        }
    }
}