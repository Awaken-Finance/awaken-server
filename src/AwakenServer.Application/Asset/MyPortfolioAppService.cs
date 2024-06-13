using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nest;
using Orleans;
using Orleans.Serialization;
using Volo.Abp.Application.Services;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.Asset;

public class MyPortfolioAppService : ApplicationService
{
    private readonly IClusterClient _clusterClient;
    private readonly ITradePairAppService _tradePairAppService;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    public async Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto)
    {
        var tradePair = await GetAsync(liquidityRecordDto.ChainId, liquidityRecordDto.Pair);
        if (tradePair == null)
        {
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        await currentTradePairGrain.AddTotalSupplyAsync(liquidityRecordDto.Type == LiquidityType.Mint ? 
            liquidityRecordDto.LpTokenAmount : -liquidityRecordDto.LpTokenAmount);
        return true;
    }
    
    public async Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        return true;
    }
    
    public async Task<TradePair> GetAsync(string chainName, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }
}