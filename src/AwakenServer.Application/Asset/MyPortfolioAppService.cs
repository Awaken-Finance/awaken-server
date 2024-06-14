using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.Asset;

[RemoteService(false)]
public class MyPortfolioAppService : ApplicationService
{
    public const string SyncedTransactionCachePrefix = "MyPortfolioSyned";
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;
    public MyPortfolioAppService(IClusterClient clusterClient, INESTRepository<TradePair, Guid> tradePairIndexRepository, INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository)
    {
        _clusterClient = clusterClient;
        _tradePairIndexRepository = tradePairIndexRepository;
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
    }

    public async Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto)
    {
        var key = $"{SyncedTransactionCachePrefix}:{liquidityRecordDto.TransactionHash}";
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        var tradePair = await GetTradePairAsync(liquidityRecordDto.ChainId, liquidityRecordDto.Pair);
        if (tradePair == null)
        {
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        await currentTradePairGrain.AddTotalSupplyAsync(liquidityRecordDto.Type == LiquidityType.Mint ? 
            liquidityRecordDto.LpTokenAmount : -liquidityRecordDto.LpTokenAmount);
        
        var currentUserLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id));
        var currentUserLiquidityGrainResult = liquidityRecordDto.Type == LiquidityType.Mint
            ? await currentUserLiquidityGrain.AddLiquidityAsync(tradePair, liquidityRecordDto)
            : await currentUserLiquidityGrain.RemoveLiquidityAsync(tradePair, liquidityRecordDto);
        // publish eto
        var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
        {
            Address = liquidityRecordDto.Address,
            TradePairId = tradePair.Id,
            LpTokenAmount = currentUserLiquidityGrainResult.Data.LpTokenAmount,
            SnapShotTime = currentUserLiquidityGrainResult.Data.LastUpdateTime.Date
        };
        var userLiquiditySnapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
            GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id, userLiquiditySnapshotGrainDto.SnapShotTime));
        await userLiquiditySnapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
        // publish eto
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }
    
    public async Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        var key = $"{SyncedTransactionCachePrefix}:{swapRecordDto.TransactionHash}";
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        await SyncSingleSwapRecordAsync(swapRecordDto);
        if (!swapRecordDto.SwapRecords.IsNullOrEmpty())
        {
            foreach (var swapRecord in swapRecordDto.SwapRecords)
            {
                ObjectMapper.Map(swapRecord, swapRecordDto);
                await SyncSingleSwapRecordAsync(swapRecordDto);
            }
        }
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }

    public async Task<bool> SyncSingleSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        var tradePair = await GetTradePairAsync(swapRecordDto.ChainId, swapRecordDto.PairAddress);
        if (tradePair == null)
        {
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        var isToken0 = swapRecordDto.SymbolIn == tradePair.Token0.Symbol;
        var total0Fee = isToken0 ? swapRecordDto.TotalFee : 0;
        var total1Fee = isToken0 ? 0 : swapRecordDto.TotalFee;
        var currentTradePairResult = await currentTradePairGrain.AddTotalFeeAsync(total0Fee, total1Fee);

        var userLiquidityList = await GetCurrentUserLiquidityIndexListAsync(tradePair.Id);
        foreach (var userLiquidity in userLiquidityList)
        {
            var userToken0Fee = total0Fee * userLiquidity.LpTokenAmount / currentTradePairResult.Data.TotalSupply;
            var userToken1Fee = total1Fee * userLiquidity.LpTokenAmount / currentTradePairResult.Data.TotalSupply;
            if (userToken0Fee == 0 && userToken1Fee == 0)
            {
                continue;
            }
            var currentLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id));
            var currentLiquidityGrainResult = await currentLiquidityGrain.AddTotalFee(userToken0Fee, userToken1Fee);
            // publish CurrentUserLiquidityEto
            
            var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
            {
                Address = userLiquidity.Address,
                TradePairId = tradePair.Id,
                LpTokenAmount = currentLiquidityGrainResult.Data.LpTokenAmount,
                SnapShotTime = currentLiquidityGrainResult.Data.LastUpdateTime.Date,
                Token0TotalFee = userToken0Fee,
                Token1TotalFee = userToken1Fee
            };
            var snapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
                GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id, currentLiquidityGrainResult.Data.LastUpdateTime.Date));
            await snapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
            // publish UserLiquiditySnapshotEto
        }
        return true;
    }


    public async Task<TradePair> GetTradePairAsync(string chainName, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }
    
    public async Task<List<CurrentUserLiquidityIndex>> GetCurrentUserLiquidityIndexListAsync(Guid tradePairId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 10000);
        return result.Item2;
    }
}