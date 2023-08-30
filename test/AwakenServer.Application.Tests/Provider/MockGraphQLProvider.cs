using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Asset;
using AwakenServer.Trade.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Provider;

public class MockGraphQLProvider :  IGraphQLProvider, ISingletonDependency
{
    private List<LiquidityRecordDto> recordList;
    private List<UserLiquidityDto> userLiquidityList;
    private List<UserTokenDto> _userTokenList;
    private List<TradePairInfoDto> tradePairInfoList;
    private List<SwapRecordDto> swapRecordList;
    private List<SyncRecordDto> syncRecordList;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
    private readonly ITokenAppService _tokenAppService;

    public MockGraphQLProvider(IObjectMapper objectMapper, INESTRepository<TradePairInfoIndex, Guid> tradePairInfoIndex,
        ITokenAppService tokenAppService)
    {
        recordList = new List<LiquidityRecordDto>();
        userLiquidityList = new List<UserLiquidityDto>();
        _userTokenList = new List<UserTokenDto>();
        tradePairInfoList = new List<TradePairInfoDto>();
        swapRecordList = new List<SwapRecordDto>();
        syncRecordList = new List<SyncRecordDto>();
        _objectMapper = objectMapper;
        _tradePairInfoIndex = tradePairInfoIndex;
        _tokenAppService = tokenAppService;
    }

    public Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListLocalAsync(GetTradePairsInfoInput input)
    {
        return Task.FromResult(new TradePairInfoDtoPageResultDto
        {
            GetTradePairInfoList = new TradePairInfoGplResultDto
            {
                TotalCount = tradePairInfoList.Count,
                Data = tradePairInfoList,
            }
        });
    }

    public async Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListAsync(GetTradePairsInfoInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePairInfoIndex>, QueryContainer>>();
        if (input.Id != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(input.Id)));
        }
        if (input.ChainId != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        }
        if (input.Address != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TradePairInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _tradePairInfoIndex.GetListAsync(Filter,
            limit: Int32.MaxValue, skip: 0);
        var totalCount = await _tradePairInfoIndex.CountAsync(Filter);
        
        list.Item2.ForEach(pair =>
        {
            var token0 = _tokenAppService.GetBySymbolCache(pair.Token0Symbol);
            var token1 = _tokenAppService.GetBySymbolCache(pair.Token1Symbol);
            pair.Token0Id = token0?.Id ?? Guid.Empty;
            pair.Token1Id = token1?.Id ?? Guid.Empty;
        });
        
        return new TradePairInfoDtoPageResultDto
        {
            GetTradePairInfoList = new TradePairInfoGplResultDto
            {
                TotalCount = totalCount.Count,
                Data = _objectMapper.Map<List<TradePairInfoIndex>, List<TradePairInfoDto>>(list.Item2)
            }
        };
    }
    public async Task<List<LiquidityRecordDto>> GetLiquidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        return recordList;
    }
    
    public async Task<List<SwapRecordDto>> GetSwapRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        return swapRecordList;
    }
    
    public async Task<List<SyncRecordDto>> GetSyncRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        return syncRecordList;
    }

    public async Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address)
    {
       return _userTokenList.Where(q => q.ChainId == chainId && q.Address == address).ToList();
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        return 2;
    }

    public async Task<long> GetLastEndHeightAsync(string chainId, string type)
    {
        return 2;
    }

    public async Task SetLastEndHeightAsync(string chainId, string type, long height)
    {
    }

    public TradePairInfoDto AddTradePairInfoAsync(TradePairInfoDto input)
    {
        tradePairInfoList.Add(input);
        return input;
    }
    
    public void AddRecord(LiquidityRecordDto dto)
    {
        recordList.Add(dto);
    }
    
    public void AddUserLiquidity(UserLiquidityDto dto)
    {
        userLiquidityList.Add(dto);
    }

    public void AddUserToken(UserTokenDto userTokenDto)
    {
        _userTokenList.Add(userTokenDto);
    }
    
    public void AddSwapRecord(SwapRecordDto dto)
    {
        swapRecordList.Add(dto);
    }
    
    public void AddSyncRecord(SyncRecordDto dto)
    {
        syncRecordList.Add(dto);
    }

    public async Task<LiquidityRecordPageResult> QueryLiquidityRecordAsync(GetLiquidityRecordIndexInput input)
    {
        var resultList = recordList.Where(q => q.ChainId.Equals(input.ChainId) && q.Address.Equals(input.Address) &&
                                               (input.Pair == null || input.Pair.Equals(q.Pair)) &&
                                               (input.Token0 == null || input.Token0.Equals(q.Token0)) &&
                                               (input.Token1 == null || input.Token1.Equals(q.Token1)) &&
                                               (input.TimestampMin == 0 || input.TimestampMin < q.Timestamp) &&
                                               (input.TimestampMax == 0 || input.TimestampMax > q.Timestamp))
            .OrderBy(q => q.Timestamp).Skip(input.SkipCount).Reverse().ToList();
        return new LiquidityRecordPageResult
        {
            TotalCount = resultList.Count,
            Data = resultList
        };
    }

    public async Task<UserLiquidityPageResultDto> QueryUserLiquidityAsync(GetUserLiquidityInput input)
    {
        var resultList = userLiquidityList.Where(q => q.ChainId.Equals(input.ChainId) && q.Address.Equals(input.Address))
            .Skip(input.SkipCount).ToList();
        return new UserLiquidityPageResultDto
        {
            TotalCount = resultList.Count,
            Data = resultList
        };
    }
}