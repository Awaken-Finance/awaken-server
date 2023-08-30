using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Entities.GameOfTrust.Es;
using AwakenServer.GameOfTrust.DTos;
using AwakenServer.GameOfTrust.DTos.Dto;
using AwakenServer.GameOfTrust.DTos.Input;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade.Dtos;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.GameOfTrust
{
    [RemoteService(IsEnabled = false)]
    public class GameOfTrustService : ApplicationService, IGameOfTrustService
    {
        private readonly INESTRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid> _gameOfTrustRepository;
        private readonly INESTRepository<GameOfTrustMarketData, Guid> _markRepository;
        private readonly INESTRepository<UserGameOfTrust, Guid> _userRepository;
        private readonly INESTRepository<GameOfTrustRecord, Guid> _recordRepository;
        private readonly IAutoObjectMappingProvider _mapper;
        private readonly IChainAppService _chainAppService;
        private readonly IPriceAppService _priceAppService;
        
        public GameOfTrustService(INESTRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid> gameOfTrustRepository,
            INESTRepository<GameOfTrustMarketData, Guid> markRepository,
            INESTRepository<UserGameOfTrust, Guid> userRepository,
            INESTRepository<GameOfTrustRecord, Guid> recordRepository, IAutoObjectMappingProvider mapper,
            IChainAppService chainAppService, IPriceAppService priceAppService)
        {
            _gameOfTrustRepository = gameOfTrustRepository;
            _markRepository = markRepository;
            _userRepository = userRepository;
            _recordRepository = recordRepository;
            _mapper = mapper;
            _chainAppService = chainAppService;
            _priceAppService = priceAppService;
        }

        /**
         * Get game of trust list by page.
         */
        public async Task<PagedResultDto<GameOfTrustDto>> GetGameOfTrustsAsync(GetGameListInput input)
        {
            var mustQuery =
                new List<Func<QueryContainerDescriptor<Entities.GameOfTrust.Es.GameOfTrust>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            if (!input.DepositTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.DepositToken.Symbol).Value(input.DepositTokenSymbol)));
            }

            if (!input.HarvestTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.HarvestToken.Symbol).Value(input.HarvestTokenSymbol)));
            }

            QueryContainer Filter(QueryContainerDescriptor<Entities.GameOfTrust.Es.GameOfTrust> f) =>
                f.Bool(b => b.Must(mustQuery));

            var list = await _gameOfTrustRepository.GetListAsync(Filter, sortExp: g => g.Pid,
                limit: input.MaxResultCount,
                skip: input.SkipCount);
            var totalCount = await _gameOfTrustRepository.CountAsync(Filter);
            return new PagedResultDto<GameOfTrustDto>
            {
                Items = ObjectMapper.Map<List<Entities.GameOfTrust.Es.GameOfTrust>, List<GameOfTrustDto>>(list.Item2),
                TotalCount = totalCount.Count
            };
        }


        /**
         *  Get game of trust info by id.
         */
        public async Task<GameOfTrustDto> GetAsync(Guid id)
        {
            var gameOfTrust = await _gameOfTrustRepository.GetAsync(id);
            return _mapper.Map<Entities.GameOfTrust.Es.GameOfTrust, GameOfTrustDto>(gameOfTrust);
        }

        /**
         *  Get game of trust token market data by contiditions.
         */
        public async Task<PagedResultDto<MarketDataDto>> GetMarketDatasAsync(GetMarketDataInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<GameOfTrustMarketData>, QueryContainer>>();
            if (!string.IsNullOrEmpty(input.ChainId))
            {
                mustQuery.Add(q => q.Term(i => i.Field(data => data.ChainId).Value(input.ChainId)));
            }

            if (input.Id.HasValue)
            {
                mustQuery.Add(q => q.Term(i => i.Field(data => data.Id).Value(input.Id)));
            }

            if (input.TimestampMin != 0)
            {
                mustQuery.Add(q =>
                    q.DateRange(i =>
                        i.Field(f => f.Timestamp)
                            .GreaterThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin))));
            }

            if (input.TimestampMax != 0)
            {
                mustQuery.Add(q =>
                    q.DateRange(i =>
                        i.Field(f =>
                                f.Timestamp)
                            .LessThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax))));
            }

            QueryContainer Filter(QueryContainerDescriptor<GameOfTrustMarketData> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _markRepository.GetListAsync(Filter, limit: input.MaxResultCount, skip: input.SkipCount,
                sortExp: m => m.Timestamp);
            var totalCount = await _markRepository.CountAsync(Filter);
            return new PagedResultDto<MarketDataDto>
            {
                Items = ObjectMapper.Map<List<GameOfTrustMarketData>, List<MarketDataDto>>(list.Item2),
                TotalCount = totalCount.Count
            };
        }

        /**
         * Get game of trust list which user participate in by conditions.
         */
        public async Task<PagedResultDto<UserGameofTrustDto>> GetUserGameOfTrustsAsync(GetUserGameOfTrustsInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<UserGameOfTrust>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
            if (!string.IsNullOrEmpty(input.DepositTokenSymbol))
            {
                mustQuery.Add(q =>
                    q.Term(i => i.Field(f => f.GameOfTrust.DepositToken.Symbol).Value(input.DepositTokenSymbol)));
            }

            if (!string.IsNullOrEmpty(input.HarvestTokenSymbol))
            {
                mustQuery.Add(q =>
                    q.Term(i => i.Field(f => f.GameOfTrust.HarvestToken.Symbol).Value(input.HarvestTokenSymbol)));
            }

            QueryContainer Filter(QueryContainerDescriptor<UserGameOfTrust> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _userRepository.GetListAsync(Filter, limit: input.MaxResultCount, skip: input.SkipCount);
            var totalCount = await _userRepository.CountAsync(Filter);
            return new PagedResultDto<UserGameofTrustDto>
            {
                Items = ObjectMapper.Map<List<UserGameOfTrust>, List<UserGameofTrustDto>>(list.Item2),
                TotalCount = totalCount.Count
            };
        }

        /**
         * Get user assert in game of trust.
         */
        public async Task<UserAssetDto> GetUserAssertAsync(GetUserAssertInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<UserGameOfTrust>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
            QueryContainer Filter(QueryContainerDescriptor<UserGameOfTrust> f) => f.Bool(b => b.Must(mustQuery));

            var list = await _userRepository.GetListAsync(Filter);
            if (list.Item1 == 0)
            {
                return new UserAssetDto();
            }

            double assetSashimi = 0;
            double assetProjectToken = 0;
            long currentBlockHeight = (await _chainAppService.GetChainStatusAsync(input.ChainId)).LatestBlockHeight;
            double totalRate = double.Parse("10000");
            
            Token sashimi = null;
            Token istar = null;
            foreach (var userInfo in list.Item2)
            {
                if (long.Parse(userInfo.ValueLocked) == 0)
                {
                    continue;
                }

                var game = await _gameOfTrustRepository.GetAsync(userInfo.GameOfTrust.Id);
                if (istar == null) istar = game.HarvestToken;
                if (sashimi == null && game.DepositToken.Id != game.HarvestToken.Id) sashimi = game.DepositToken;

                double unClaimedFine = 0;
                
                if (game.UnlockHeight > 0 && currentBlockHeight >= (game.UnlockHeight + game.UnlockCycle))
                {
                    currentBlockHeight = game.UnlockHeight + game.UnlockCycle;
                    if (double.Parse(userInfo.ReceivedFineAmount) == 0)
                    {
                        unClaimedFine = double.Parse(userInfo.ValueLocked) /
                            double.Parse(game.TotalValueLocked) * double.Parse(game.FineAmount);
                    }
                }
 
                if (userInfo.GameOfTrust.DepositToken.Symbol.Equals(userInfo.GameOfTrust.HarvestToken.Symbol))
                {
                    if (game.UnlockHeight != 0)
                    {
                        double unClaimed = double.Parse(game.RewardRate) / totalRate *
                                           double.Parse(userInfo.ValueLocked) *
                                           double.Parse((currentBlockHeight - game.UnlockHeight).ToString()) /
                                           double.Parse(game.UnlockCycle.ToString()) +
                                           double.Parse(userInfo.ValueLocked) -
                                           double.Parse(userInfo.ReceivedAmount);
                        assetProjectToken += unClaimed + unClaimedFine;
                    }
                    else
                    {
                        assetProjectToken += Convert.ToDouble(userInfo.ValueLocked);
                    }
                }
                else
                {
                    if (game.UnlockHeight != 0)
                    {
                        double unLockAmount = double.Parse(userInfo.ValueLocked) *
                                              (double.Parse((currentBlockHeight - game.UnlockHeight).ToString()) /
                                               double.Parse(game.UnlockCycle.ToString()));
                        double unLockReward = unLockAmount * (totalRate + double.Parse(game.RewardRate)) / totalRate;

                        double surplusLockAmount = Convert.ToDouble(userInfo.ValueLocked) - unLockAmount;
                        assetProjectToken += unLockReward - double.Parse(userInfo.ReceivedAmount);
                        assetSashimi += surplusLockAmount;
                    }
                    else
                    {
                        assetSashimi += Convert.ToDouble(userInfo.ValueLocked);
                    }
                }
            }
            
            var sashimiPrice = sashimi == null
                ? 0
                : double.Parse(await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
                {
                    Symbol = sashimi.Symbol,
                    ChainId = sashimi.ChainId,
                    TokenId = sashimi.Id
                }));
            var istarPrice = istar == null
                ? 0
                : double.Parse(await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
                {
                    Symbol = istar.Symbol,
                    ChainId = istar.ChainId,
                    TokenId = istar.Id
                }));
            var btcPrice = double.Parse(await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.BTC
            }));
            
            var valueUSD = assetSashimi * sashimiPrice + assetProjectToken * istarPrice;
            return new UserAssetDto
            {
                AssetUSD = valueUSD,
                AssetBTC = btcPrice == 0 ? 0 : valueUSD / btcPrice
            };
        }

        /**
         * Get user operation record in game of trust.
         */
        public async Task<PagedResultDto<GetUserGameOfTrustRecordDto>> GetUserGameOfTrustRecord(
            GetUserGameOfTrustRecordInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<GameOfTrustRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            if (!input.Address.IsNullOrEmpty())
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.Address).Value(input.Address)));
            }

            if (!input.DepositTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.GameOfTrust.DepositToken.Symbol).Value(input.DepositTokenSymbol)));
            }

            if (!input.HarvestTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.GameOfTrust.HarvestToken.Symbol).Value(input.HarvestTokenSymbol)));
            }

            if (input.TimestampMin != 0)
            {
                mustQuery.Add(q =>
                {
                    return q.DateRange(i =>
                        i.Field(f =>
                            f.Timestamp).GreaterThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin)));
                });
            }

            if (input.TimestampMax != 0)
            {
                mustQuery.Add(q =>
                    q.DateRange(i =>
                        i.Field(f =>
                            f.Timestamp).LessThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax))));
            }

            if (input.UnlockMarketCap != 0)
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.GameOfTrust.UnlockMarketCap).Value(input.UnlockMarketCap)));
            }

            if (input.type != null)
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.Type).Value(input.type)));
            }

            QueryContainer Filter(QueryContainerDescriptor<GameOfTrustRecord> f) =>
                f.Bool(b =>
                    b.Must(mustQuery));

            var list = await _recordRepository.GetListAsync(Filter, sortExp: k => k.Timestamp,
                limit: input.MaxResultCount, skip: input.SkipCount);
            var totalCount = await _recordRepository.CountAsync(Filter);
            return new PagedResultDto<GetUserGameOfTrustRecordDto>
            {
                Items = ObjectMapper.Map<List<GameOfTrustRecord>, List<GetUserGameOfTrustRecordDto>>(list.Item2),
                TotalCount = totalCount.Count
            };
        }

        /**
         * Get marketcap list of game of trust.
         */
        public async Task<ListResultDto<MarketCapsDto>> GetMarketCapsAsync(GetMarketCapsInput input)
        {
            var mustQuery =
                new List<Func<QueryContainerDescriptor<Entities.GameOfTrust.Es.GameOfTrust>, QueryContainer>>();
            mustQuery.Add(q =>
                q.Term(i =>
                    i.Field(f =>
                        f.ChainId).Value(input.ChainId)));

            if (!input.DepositTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.DepositToken.Symbol).Value(input.DepositTokenSymbol)));
            }

            if (!input.HarvestTokenSymbol.IsNullOrEmpty())
            {
                mustQuery.Add(q =>
                    q.Term(i =>
                        i.Field(f =>
                            f.HarvestToken.Symbol).Value(input.HarvestTokenSymbol)));
            }

            QueryContainer Filter(QueryContainerDescriptor<Entities.GameOfTrust.Es.GameOfTrust> f) =>
                f.Bool(b =>
                    b.Must(mustQuery));

            var totalCount = await _gameOfTrustRepository.CountAsync(Filter);
            var list = await _gameOfTrustRepository.GetListAsync(Filter, sortExp: K => K.Pid,
                limit: (int) totalCount.Count);
            return new ListResultDto<MarketCapsDto>
            {
                Items = ObjectMapper.Map<List<Entities.GameOfTrust.Es.GameOfTrust>, List<MarketCapsDto>>(list.Item2)
            };
        }
    }
}