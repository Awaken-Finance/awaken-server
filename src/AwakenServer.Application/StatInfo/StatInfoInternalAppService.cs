using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.StatInfo;
using AwakenServer.StatInfo;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using SwapRecord = AwakenServer.Trade.SwapRecord;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.DataInfo;

public class StatInfoInternalAppService : ApplicationService, IStatInfoInternalAppService
{
    public const string SyncedTransactionCachePrefix = "StatInfoSynced";
    private readonly ITradePairAppService _tradePairAppService;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<TokenStatInfoIndex, Guid> _tokenStatInfoIndexRepository;
    private readonly INESTRepository<TransactionHistoryIndex, Guid> _transactionIndexRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;
    private readonly IOptionsSnapshot<StatInfoOptions> _statInfoOptions;

    private string AddVersionToKey(string baseKey, string version)
    {
        return $"{baseKey}:{version}";
    }
    public async Task<bool> CreateLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto, string dataVersion)
    {
        var key = AddVersionToKey($"{SyncedTransactionCachePrefix}:{nameof(LiquidityRecord)}:{liquidityRecordDto.TransactionHash}", dataVersion);
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        // insert transaction
        var tradePair = await GetTradePairAsync(liquidityRecordDto.ChainId, liquidityRecordDto.Pair);
        if (tradePair == null)
        {
            return false;
        }
        var transactionHistory = new TransactionHistoryEto()
        {
            TradePair = tradePair,
            TransactionType = liquidityRecordDto.Type == LiquidityType.Mint ? TransactionType.Add : TransactionType.Remove,
            TransactionHash = liquidityRecordDto.TransactionHash,
            Timestamp = liquidityRecordDto.Timestamp,
            ChainId = liquidityRecordDto.ChainId,
            Version = dataVersion
        };
        var isTokenReversed = tradePair.Token0.Symbol != liquidityRecordDto.Token0;
        transactionHistory.Token0Amount = isTokenReversed
            ? liquidityRecordDto.Token1Amount.ToDecimalsString(tradePair.Token0.Decimals)
            : liquidityRecordDto.Token0Amount.ToDecimalsString(tradePair.Token0.Decimals);
        transactionHistory.Token1Amount = isTokenReversed
            ? liquidityRecordDto.Token0Amount.ToDecimalsString(tradePair.Token1.Decimals)
            : liquidityRecordDto.Token1Amount.ToDecimalsString(tradePair.Token1.Decimals);
        var priceTuple = await _tokenPriceProvider.GetUSDPriceAsync(liquidityRecordDto.ChainId, tradePair.Id, tradePair.Token0.Symbol, tradePair.Token1.Symbol);
        transactionHistory.ValueInUsd = priceTuple.Item1 * Double.Parse(transactionHistory.Token0Amount) +
                                        priceTuple.Item2 * Double.Parse(transactionHistory.Token1Amount);
        await _distributedEventBus.PublishAsync(transactionHistory);
        
        // transaction count
        await IncTransactionCount(tradePair.ChainId, tradePair.Address, tradePair.Token0.Symbol,
            tradePair.Token1.Symbol, dataVersion);
        
        await _syncedTransactionIdCache.SetAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(7)
        });
        return true;
    }
    
    public async Task<bool> CreateSwapRecordAsync(SwapRecordDto swapRecordDto, string dataVersion)
    {
        var key = AddVersionToKey($"{SyncedTransactionCachePrefix}:{nameof(SwapRecord)}:{swapRecordDto.TransactionHash}", dataVersion);
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        await SyncSingleSwapRecordAsync(swapRecordDto, dataVersion);
        if (!swapRecordDto.SwapRecords.IsNullOrEmpty())
        {
            foreach (var swapRecord in swapRecordDto.SwapRecords)
            {
                ObjectMapper.Map(swapRecord, swapRecordDto);
                await SyncSingleSwapRecordAsync(swapRecordDto, dataVersion);
            }
        }
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }

    private async Task SyncSingleSwapRecordAsync(SwapRecordDto swapRecordDto, string dataVersion)
    {
        // insert transaction
        var tradePair = await GetTradePairAsync(swapRecordDto.ChainId, swapRecordDto.PairAddress);
        if (tradePair == null)
        {
            return;
        }

        var isSell = swapRecordDto.SymbolIn == tradePair.Token0.Symbol;
        var transactionHistory = new TransactionHistoryEto()
        {
            TradePair = tradePair,
            TransactionType = TransactionType.Trade,
            TransactionHash = swapRecordDto.TransactionHash,
            Timestamp = swapRecordDto.Timestamp,
            ChainId = swapRecordDto.ChainId,
            Side = isSell ? TradeSide.Sell : TradeSide.Buy
        };
        transactionHistory.Token0Amount = isSell
            ? swapRecordDto.AmountIn.ToDecimalsString(tradePair.Token0.Decimals)
            : swapRecordDto.AmountOut.ToDecimalsString(tradePair.Token0.Decimals);
        transactionHistory.Token1Amount = isSell 
            ? swapRecordDto.AmountOut.ToDecimalsString(tradePair.Token1.Decimals)
            : swapRecordDto.AmountIn.ToDecimalsString(tradePair.Token1.Decimals);
        var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(
            swapRecordDto.ChainId, tradePair.Token0.Symbol);
        var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(
            swapRecordDto.ChainId, tradePair.Token1.Symbol);
        transactionHistory.ValueInUsd = token0Price * Double.Parse(transactionHistory.Token0Amount);
        transactionHistory.Version = dataVersion;
        
        // pool lpFee/volume
        var poolStatInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(swapRecordDto.ChainId, swapRecordDto.PairAddress), dataVersion));
        var poolStatInfoGrainDtoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoGrainDtoResult.Data.PairAddress = swapRecordDto.PairAddress;
        poolStatInfoGrainDtoResult.Data.VolumeInUsd7d = 0;//todo
        poolStatInfoGrainDtoResult.Data.VolumeInUsd24h = 0;//todo
        poolStatInfoGrainDtoResult.Data.TransactionCount++;
        poolStatInfoGrainDtoResult = await poolStatInfoGrain.AddOrUpdateAsync(poolStatInfoGrainDtoResult.Data);
        poolStatInfoGrainDtoResult.Data.Version = dataVersion;
        if (poolStatInfoGrainDtoResult.Success)
        {
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<PoolStatInfoGrainDto, PoolStatInfoEto>(poolStatInfoGrainDtoResult.Data));
        }
        
        var lpFeeAmount = swapRecordDto.TotalFee.ToDecimalsString(isSell ? tradePair.Token0.Decimals : tradePair.Token1.Decimals);
        var snapshotEto = new StatInfoSnapshotEto
        {
            Version = dataVersion,
            StatType = 2,
            PairAddress = tradePair.Address,
            Timestamp = swapRecordDto.Timestamp,
            VolumeInUsd = transactionHistory.ValueInUsd,
            LpFeeInUsd = Double.Parse(lpFeeAmount) * (isSell ? token0Price : token1Price)
        };
        await _distributedEventBus.PublishAsync(snapshotEto);

        // token volume
        await UpdateTokenVolume(swapRecordDto.ChainId, tradePair.Token0.Symbol, 
            transactionHistory.ValueInUsd, swapRecordDto.Timestamp, dataVersion);
        await UpdateTokenVolume(swapRecordDto.ChainId, tradePair.Token1.Symbol, 
            Double.Parse(transactionHistory.Token1Amount) * token1Price, swapRecordDto.Timestamp, dataVersion);
        
        // global volume
        var globalSnapshotEto = new StatInfoSnapshotEto
        {
            Version = dataVersion,
            StatType = 0,
            Timestamp = swapRecordDto.Timestamp,
            VolumeInUsd = transactionHistory.ValueInUsd,
        };
        await _distributedEventBus.PublishAsync(globalSnapshotEto);
    }

    public async Task<bool> CreateSyncRecordAsync(SyncRecordDto syncRecordDto, string dataVersion)
    {
        var key = AddVersionToKey($"{SyncedTransactionCachePrefix}:{nameof(syncRecordDto)}:{syncRecordDto.TransactionHash}", dataVersion);
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        var tradePair = await GetTradePairAsync(syncRecordDto.ChainId, syncRecordDto.PairAddress);
        if (tradePair == null)
        {
            return false;
        }
        var isTokenReversed = tradePair.Token0.Symbol != syncRecordDto.SymbolA;
        var token0Amount = isTokenReversed
            ? syncRecordDto.ReserveB.ToDecimalsString(tradePair.Token0.Decimals)
            : syncRecordDto.ReserveA.ToDecimalsString(tradePair.Token0.Decimals);
        var token1Amount = isTokenReversed
            ? syncRecordDto.ReserveA.ToDecimalsString(tradePair.Token1.Decimals)
            : syncRecordDto.ReserveB.ToDecimalsString(tradePair.Token1.Decimals);
        var (token0Price, token1Price) = await _tokenPriceProvider.GetUSDPriceAsync(syncRecordDto.ChainId, tradePair.Id,
            tradePair.Token0.Symbol, tradePair.Token1.Symbol);
        
        // pool tvl/price
        var poolStateInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(syncRecordDto.ChainId, syncRecordDto.PairAddress), dataVersion));
        var poolStateInfoGrainDtoResult = await poolStateInfoGrain.GetAsync();
        var oldValueLocked0 = poolStateInfoGrainDtoResult.Data.ValueLocked0;
        var oldValueLocked1 = poolStateInfoGrainDtoResult.Data.ValueLocked1;
        var oldTvl = poolStateInfoGrainDtoResult.Data.Tvl;
        
        poolStateInfoGrainDtoResult.Data.PairAddress = tradePair.Address;
        poolStateInfoGrainDtoResult.Data.ValueLocked0 = double.Parse(token0Amount);
        poolStateInfoGrainDtoResult.Data.ValueLocked1 = double.Parse(token1Amount);
        poolStateInfoGrainDtoResult.Data.Tvl =
            double.Parse(token0Amount) * token0Price + double.Parse(token1Amount) * token1Price;
        poolStateInfoGrainDtoResult.Data.Price = double.Parse(token1Amount) / double.Parse(token0Amount);
        poolStateInfoGrainDtoResult.Data.LastUpdateTime = syncRecordDto.Timestamp;
        poolStateInfoGrainDtoResult = await poolStateInfoGrain.AddOrUpdateAsync(poolStateInfoGrainDtoResult.Data);
        poolStateInfoGrainDtoResult.Data.Version = dataVersion;
        if (poolStateInfoGrainDtoResult.Success) {
            await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<PoolStatInfoGrainDto, PoolStatInfoEto>(poolStateInfoGrainDtoResult.Data));
            await _distributedEventBus.PublishAsync(
                new StatInfoSnapshotEto()
                {
                    Version = dataVersion,
                    StatType = 2,
                    PairAddress = syncRecordDto.PairAddress,
                    Price = poolStateInfoGrainDtoResult.Data.Price,
                    Tvl = poolStateInfoGrainDtoResult.Data.Tvl,
                    Timestamp = syncRecordDto.Timestamp
                });
        }

        // follow token price/tvl
        var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(
            AddVersionToKey(GrainIdHelper.GenerateGrainId(syncRecordDto.ChainId, tradePair.Token0.Symbol), dataVersion));
        var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
        tokenStatInfoGrainDtoResult.Data.Symbol = tradePair.Token0.Symbol;
        tokenStatInfoGrainDtoResult.Data.ValueLocked += double.Parse(token0Amount) - oldValueLocked0;
        tokenStatInfoGrainDtoResult.Data.Tvl = tokenStatInfoGrainDtoResult.Data.ValueLocked * token0Price;
        tokenStatInfoGrainDtoResult.Data.LastUpdateTime = syncRecordDto.Timestamp;
        tokenStatInfoGrainDtoResult.Data.PriceInUsd = 0; //todo
        tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
        tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
        if (tokenStatInfoGrainDtoResult.Success)
        {
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<TokenStatInfoGrainDto, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
            await _distributedEventBus.PublishAsync(new StatInfoSnapshotEto()
            {
                Version = dataVersion,
                StatType = 1,
                Symbol = tradePair.Token0.Symbol,
                Price = tokenStatInfoGrainDtoResult.Data.PriceInUsd,
                Tvl = tokenStatInfoGrainDtoResult.Data.Tvl,
                Timestamp = syncRecordDto.Timestamp
            });
        }

        var token1StatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(
            AddVersionToKey(GrainIdHelper.GenerateGrainId(syncRecordDto.ChainId, tradePair.Token1.Symbol), dataVersion));
        var token1StatInfoGrainDtoResult = await token1StatInfoGrain.GetAsync();
        token1StatInfoGrainDtoResult.Data.Symbol = tradePair.Token1.Symbol;
        token1StatInfoGrainDtoResult.Data.ValueLocked += double.Parse(token1Amount) - oldValueLocked1;
        token1StatInfoGrainDtoResult.Data.Tvl = token1StatInfoGrainDtoResult.Data.ValueLocked * token1Price;
        token1StatInfoGrainDtoResult.Data.LastUpdateTime = syncRecordDto.Timestamp;
        token1StatInfoGrainDtoResult.Data.PriceInUsd = 0; //todo
        token1StatInfoGrainDtoResult = await token1StatInfoGrain.AddOrUpdateAsync(token1StatInfoGrainDtoResult.Data);
        token1StatInfoGrainDtoResult.Data.Version = dataVersion;
        if (token1StatInfoGrainDtoResult.Success)
        {
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<TokenStatInfoGrainDto, TokenStatInfoEto>(token1StatInfoGrainDtoResult.Data));
            await _distributedEventBus.PublishAsync(new StatInfoSnapshotEto()
            {
                Version = dataVersion,
                StatType = 1,
                Symbol = tradePair.Token1.Symbol,
                Price = poolStateInfoGrainDtoResult.Data.Price,
                Tvl = poolStateInfoGrainDtoResult.Data.Tvl,
                Timestamp = poolStateInfoGrainDtoResult.Data.LastUpdateTime
            });
        }

        
        // global tvl
        var globalGrain = _clusterClient.GetGrain<IGlobalStatInfoGrain>(AddVersionToKey(syncRecordDto.ChainId, dataVersion));
        var globalGrainDtoResult = await globalGrain.AddTvlAsync(poolStateInfoGrainDtoResult.Data.Tvl - oldTvl);
        var globalSnapshotEto = new StatInfoSnapshotEto()
        {
            Version = dataVersion,
            StatType = 0,
            Tvl = globalGrainDtoResult.Data.Tvl,
            Timestamp = syncRecordDto.Timestamp
        };
        await _distributedEventBus.PublishAsync(globalSnapshotEto);
        
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }

    public async Task RefreshTvlAsync(string chainId, string dataVersion)
    {
        var pairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
        {
            ChainId = chainId,
            MaxResultCount = 1000
        });
        var totalTvl = 0.0;
        foreach (var tradePair in pairs.Items)
        {
            totalTvl += tradePair.TVL;
        }
        var globalGrain = _clusterClient.GetGrain<IGlobalStatInfoGrain>(AddVersionToKey(chainId, dataVersion));
        var globalGrainDtoResult = await globalGrain.UpdateTvlAsync(totalTvl);
        var globalSnapshotEto = new StatInfoSnapshotEto()
        {
            StatType = 0,
            Tvl = globalGrainDtoResult.Data.Tvl,
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
        await _distributedEventBus.PublishAsync(globalSnapshotEto);
    }

    public async Task UpdateTokenFollowPairAsync(string chainId, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<TokenStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result =  await _tokenStatInfoIndexRepository.GetListAsync(Filter);
        var notNeedInitSymbols = result.Item2.Where(t => !t.FollowPairAddress.IsNullOrWhiteSpace()).Select(t => t.Symbol).ToList();

        var pairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
        {
            ChainId = chainId,
            MaxResultCount = 1000
        });
        var pricingNodeMap = new Dictionary<string, PricingNode>();
        foreach (var tradePair in pairs.Items)
        {
            UpdatePricingNode(tradePair, tradePair.Token0.Symbol, tradePair.Token1.Symbol, pricingNodeMap);
            UpdatePricingNode(tradePair, tradePair.Token1.Symbol, tradePair.Token0.Symbol, pricingNodeMap);
        }

        foreach (var pricingNodePair in pricingNodeMap)
        {
            if (notNeedInitSymbols.Contains(pricingNodePair.Key))
            {
                continue;
            }
            var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(
                AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, pricingNodePair.Key), dataVersion));
            var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
            tokenStatInfoGrainDtoResult.Data.Symbol = pricingNodePair.Key;
            tokenStatInfoGrainDtoResult.Data.FollowPairAddress = pricingNodePair.Value.FromTradePairAddress;
            await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);

            await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfoGrainDto, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
        }
    }

    public async Task ClearOldTransactionHistoryAsync(string chainId, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TransactionHistoryIndex>, QueryContainer>>();
       
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        QueryContainer Filter(QueryContainerDescriptor<TransactionHistoryIndex> f) => f.Bool(b => b.Must(mustQuery));

        int pageSize = 1000; 
        int currentPage = 1;
        while (true)
        {
            var pageData = await _transactionIndexRepository.GetSortListAsync(Filter, sortFunc: s => s.Descending(t => t.Timestamp), 
                skip: currentPage * pageSize, limit: pageSize);
            if (pageData.Item2.Count == 0)
            {
                break;
            }
            await _transactionIndexRepository.BulkDeleteAsync(pageData.Item2);
            currentPage++;
        }
        
    }

    private void UpdatePricingNode(TradePairIndexDto tradePair, string symbol, string fromTokenSymbol, Dictionary<string, PricingNode> pricingNodeMap)
    {
        pricingNodeMap.TryGetValue(symbol, out var pricingNode);
        if (pricingNode != null)
        {
            var oldFromSymbolIndex = _statInfoOptions.Value.StableCoinPriority.IndexOf(pricingNode.FromTokenSymbol);
            var curFromSymbolIndex = _statInfoOptions.Value.StableCoinPriority.IndexOf(fromTokenSymbol);
            if (curFromSymbolIndex > oldFromSymbolIndex || curFromSymbolIndex == oldFromSymbolIndex && tradePair.TVL > pricingNode.Tvl)
            {
                pricingNode.Tvl = tradePair.TVL;
                pricingNode.FromTokenSymbol = fromTokenSymbol;
                pricingNode.FromTradePairAddress = tradePair.Address;
            }
        }
        else
        {
            pricingNodeMap[symbol] = new PricingNode
            {
                FromTokenSymbol = fromTokenSymbol,
                FromTradePairAddress = tradePair.Address,
                Tvl = tradePair.TVL
            };
        }
    }

    private async Task IncTransactionCount(string chainId, string pairAddress, string symbol0, string symbol1, string dataVersion)
    {
        var poolStatInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, pairAddress), dataVersion));
        var poolStatInfoGrainDtoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoGrainDtoResult.Data.PairAddress = pairAddress;
        poolStatInfoGrainDtoResult.Data.TransactionCount++;
        poolStatInfoGrainDtoResult = await poolStatInfoGrain.AddOrUpdateAsync(poolStatInfoGrainDtoResult.Data);
        poolStatInfoGrainDtoResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<PoolStatInfoGrainDto, PoolStatInfoEto>(poolStatInfoGrainDtoResult.Data));
        
        await IncTokenTransactionCount(chainId, symbol0, dataVersion);
        await IncTokenTransactionCount(chainId, symbol1, dataVersion);
    }

    private async Task IncTokenTransactionCount(string chainId, string symbol, string dataVersion)
    {
        var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, symbol), dataVersion));
        var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
        tokenStatInfoGrainDtoResult.Data.Symbol = symbol;
        tokenStatInfoGrainDtoResult.Data.TransactionCount++;
        tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
        tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfoGrainDto, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
    }

    private async Task UpdateTokenVolume(string chainId, string symbol, double volumeInUsd, long timestamp, string dataVersion)
    {
        var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, symbol), dataVersion));
        var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
        tokenStatInfoGrainDtoResult.Data.Symbol = symbol;
        tokenStatInfoGrainDtoResult.Data.TransactionCount++;
        tokenStatInfoGrainDtoResult.Data.VolumeInUsd24h = 0; // todo
        tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
        tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfoGrainDto, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
        
        var tokenSnapshotEto = new StatInfoSnapshotEto
        {
            StatType = 1,
            Symbol = symbol,
            Timestamp = timestamp,
            VolumeInUsd = volumeInUsd,
        };
        await _distributedEventBus.PublishAsync(tokenSnapshotEto);
    }

    private async Task<TradePair> GetTradePairAsync(string chainName, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }
    
    private class PricingNode
    {
        public string FromTokenSymbol { get; set; }
        public string FromTradePairAddress { get; set; }
        public double Tvl { get; set; }
    }
}