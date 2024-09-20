using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.StatInfo;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
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
using ITokenPriceProvider = AwakenServer.Trade.ITokenPriceProvider;
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
    private readonly INESTRepository<PoolStatInfoIndex, Guid> _poolStatInfoIndexRepository;
    private readonly INESTRepository<TransactionHistoryIndex, Guid> _transactionIndexRepository;
    private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IPriceAppService _priceAppService;
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
    
    private async Task IncTransactionCount(string chainId, string pairAddress, string symbol0, string symbol1, string dataVersion)
    {
        var poolStatInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, pairAddress), dataVersion));
        var poolStatInfoGrainDtoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoGrainDtoResult.Data.PairAddress = pairAddress;
        poolStatInfoGrainDtoResult.Data.TransactionCount++;
        poolStatInfoGrainDtoResult = await poolStatInfoGrain.AddOrUpdateAsync(poolStatInfoGrainDtoResult.Data);
        poolStatInfoGrainDtoResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<PoolStatInfo, PoolStatInfoEto>(poolStatInfoGrainDtoResult.Data));
        
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
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfo, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
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
        transactionHistory.ValueInUsd = token0Price > 0 ? token0Price * Double.Parse(transactionHistory.Token0Amount)
                : token1Price * Double.Parse(transactionHistory.Token1Amount);
        transactionHistory.Version = dataVersion;
        
        // pool lpFee/volume
        var poolStatInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(swapRecordDto.ChainId, swapRecordDto.PairAddress), dataVersion));
        var poolStatInfoGrainDtoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoGrainDtoResult.Data.PairAddress = swapRecordDto.PairAddress;

        var statInfoSnapshot7Day = await GetLast7DaySnapshotListAsync(new StatInfoSnapshot
        {
            StatType = 2,
            Version = dataVersion,
            ChainId = swapRecordDto.ChainId,
            PairAddress = swapRecordDto.PairAddress
        });
        poolStatInfoGrainDtoResult.Data.VolumeInUsd7d = statInfoSnapshot7Day.Sum(t => t.VolumeInUsd)
                                                        + transactionHistory.ValueInUsd;
        var statInfoSnapshot24H = await GetLast24hSnapshotListAsync(new StatInfoSnapshot
        {
            StatType = 2,
            Version = dataVersion,
            ChainId = swapRecordDto.ChainId,
            PairAddress = swapRecordDto.PairAddress
        });
        poolStatInfoGrainDtoResult.Data.VolumeInUsd24h = statInfoSnapshot24H.Sum(t => t.VolumeInUsd)
                                                         + transactionHistory.ValueInUsd;
        poolStatInfoGrainDtoResult.Data.TransactionCount++;
        poolStatInfoGrainDtoResult.Data.LastUpdateTime = swapRecordDto.Timestamp;
        poolStatInfoGrainDtoResult = await poolStatInfoGrain.AddOrUpdateAsync(poolStatInfoGrainDtoResult.Data);
        poolStatInfoGrainDtoResult.Data.Version = dataVersion;
        if (poolStatInfoGrainDtoResult.Success)
        {
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<PoolStatInfo, PoolStatInfoEto>(poolStatInfoGrainDtoResult.Data));
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
        await UpdateTokenVolumeAsync(swapRecordDto.ChainId, tradePair.Token0.Symbol, 
            transactionHistory.ValueInUsd, swapRecordDto.Timestamp, dataVersion);
        await UpdateTokenVolumeAsync(swapRecordDto.ChainId, tradePair.Token1.Symbol, 
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
    
    private async Task UpdateTokenVolumeAsync(string chainId, string symbol, double volumeInUsd, long timestamp, string dataVersion)
    {
        var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, symbol), dataVersion));
        var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
        tokenStatInfoGrainDtoResult.Data.Symbol = symbol;
        tokenStatInfoGrainDtoResult.Data.TransactionCount++;
        
        var statInfoSnapshot24H = await GetLast24hSnapshotListAsync(new StatInfoSnapshot
        {
            StatType = 1,
            Version = dataVersion,
            ChainId = chainId,
            Symbol = symbol
        });
        tokenStatInfoGrainDtoResult.Data.VolumeInUsd24h = statInfoSnapshot24H.Sum(t => t.VolumeInUsd) + volumeInUsd;
        tokenStatInfoGrainDtoResult.Data.LastUpdateTime = timestamp;
        tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
        tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfo, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
        
        var tokenSnapshotEto = new StatInfoSnapshotEto
        {
            StatType = 1,
            Symbol = symbol,
            Timestamp = timestamp,
            VolumeInUsd = volumeInUsd,
            Version = dataVersion
        };
        await _distributedEventBus.PublishAsync(tokenSnapshotEto);
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
            ObjectMapper.Map<PoolStatInfo, PoolStatInfoEto>(poolStateInfoGrainDtoResult.Data));
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

        // token tvl/priceInUsd
        await UpdateTokenTvlAndPriceAsync(syncRecordDto, tradePair.Token0.Symbol, tradePair.Token1.Symbol,
            double.Parse(token0Amount) - oldValueLocked0, token0Price, poolStateInfoGrainDtoResult.Data.Price,
            dataVersion);
        await UpdateTokenTvlAndPriceAsync(syncRecordDto, tradePair.Token1.Symbol, tradePair.Token0.Symbol,
            double.Parse(token1Amount) - oldValueLocked1, token1Price, 1.0 / poolStateInfoGrainDtoResult.Data.Price,
            dataVersion);

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

    private async Task UpdateTokenTvlAndPriceAsync(SyncRecordDto syncRecordDto, string symbol, string otherSymbol, 
        double addValueLocked, double curTokenPriceInUsd, double tradePairPrice, string dataVersion)
    {
        var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(
            AddVersionToKey(GrainIdHelper.GenerateGrainId(syncRecordDto.ChainId, symbol), dataVersion));
        var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
        tokenStatInfoGrainDtoResult.Data.Symbol = symbol;
        tokenStatInfoGrainDtoResult.Data.ValueLocked += addValueLocked;
        tokenStatInfoGrainDtoResult.Data.Tvl = tokenStatInfoGrainDtoResult.Data.ValueLocked * curTokenPriceInUsd;
        tokenStatInfoGrainDtoResult.Data.LastUpdateTime = syncRecordDto.Timestamp;
        if (tokenStatInfoGrainDtoResult.Data.FollowPairAddress == syncRecordDto.PairAddress)
        {
            tokenStatInfoGrainDtoResult.Data.PriceInUsd = await GetStatInfoTokenPriceInUsdAsync(symbol,
                otherSymbol, tradePairPrice, syncRecordDto.Timestamp);
        }

        tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
        tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
        if (tokenStatInfoGrainDtoResult.Success)
        {
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<TokenStatInfo, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
            await _distributedEventBus.PublishAsync(new StatInfoSnapshotEto()
            {
                Version = dataVersion,
                StatType = 1,
                Symbol = tokenStatInfoGrainDtoResult.Data.Symbol,
                Tvl = tokenStatInfoGrainDtoResult.Data.Tvl,
                Timestamp = syncRecordDto.Timestamp
            });
        }
    }

    private async Task<double> GetStatInfoTokenPriceInUsdAsync(string symbol, string otherSymbol, double price, long timestamp)
    {
        if (_statInfoOptions.Value.StableCoinPriority.Contains(symbol))
        {
            return await GetHistoryPriceInUsdAsync(symbol, timestamp);
        }
        if (_statInfoOptions.Value.StableCoinPriority.Contains(otherSymbol))
        {
            return await GetHistoryPriceInUsdAsync(otherSymbol, timestamp) * price;
        }
        return await GetHistoryPriceInUsdAsync(symbol, timestamp);
    }

    private async Task<double> GetHistoryPriceInUsdAsync(string symbol, long timestamp)
    {
        var historyList = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
        {
            new (){
                Symbol = symbol,
                DateTime = DateTimeHelper.FromUnixTimeMilliseconds(timestamp)
            }
        });
        if (historyList == null || historyList.Items.Count == 0)
        {
            return 0;
        }
        return double.Parse(historyList.Items[0].PriceInUsd.ToString());
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
    
    public async Task RefreshTokenStatInfoAsync(string chainId, string dataVersion)
    {
        var pairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
        {
            ChainId = chainId,
            MaxResultCount = 1000
        });
        var poolCountMap = new Dictionary<string, long>();
        foreach (var pair in pairs.Items)
        {
            poolCountMap[pair.Token0.Symbol] = poolCountMap.GetValueOrDefault(pair.Token0.Symbol, 0) + 1;
            poolCountMap[pair.Token1.Symbol] = poolCountMap.GetValueOrDefault(pair.Token1.Symbol, 0) + 1;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<TokenStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result =  await _tokenStatInfoIndexRepository.GetListAsync(Filter);
        foreach (var tokenStatInfoIndex in result.Item2)
        {
            var tokenStatInfoGrain = _clusterClient.GetGrain<ITokenStatInfoGrain>(
                AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, tokenStatInfoIndex.Symbol), dataVersion));
            var tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.GetAsync();
            tokenStatInfoGrainDtoResult.Data.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tokenStatInfoGrainDtoResult.Data.PoolCount = poolCountMap.GetValueOrDefault(tokenStatInfoIndex.Symbol);
            var statInfoSnapshot24HList = await GetLast24hSnapshotListAsync(new StatInfoSnapshot
            {
                StatType = 1,
                Version = dataVersion,
                ChainId = chainId,
                Symbol = tokenStatInfoIndex.Symbol
            });
            tokenStatInfoGrainDtoResult.Data.VolumeInUsd24h = statInfoSnapshot24HList.Sum(t => t.VolumeInUsd);
            var statInfoSnapshot24H = await GetLatestSnapshotBefore24hAsync(new StatInfoSnapshot()
            {
                StatType = 1,
                Version = dataVersion,
                ChainId = chainId,
                Symbol = tokenStatInfoIndex.Symbol
            });
            if (statInfoSnapshot24H?.PriceInUsd > 0)
            {
                tokenStatInfoGrainDtoResult.Data.PricePercentChange24h =
                    (tokenStatInfoGrainDtoResult.Data.PriceInUsd - statInfoSnapshot24H.PriceInUsd) * 100 /
                    statInfoSnapshot24H.PriceInUsd;
            }
            tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);
            tokenStatInfoGrainDtoResult.Data.Version = dataVersion;
            if (tokenStatInfoGrainDtoResult.Success)
            {
                await _distributedEventBus.PublishAsync(
                    ObjectMapper.Map<TokenStatInfo, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
            }
        }
    }
    
    public async Task RefreshPoolStatInfoAsync(string chainId, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<PoolStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result =  await _poolStatInfoIndexRepository.GetListAsync(Filter);
        foreach (var poolStatInfoIndex in result.Item2)
        {
            var poolStatInfoGrain = _clusterClient.GetGrain<IPoolStatInfoGrain>(
                AddVersionToKey(GrainIdHelper.GenerateGrainId(chainId, poolStatInfoIndex.PairAddress), dataVersion));
            var poolStatInfoGrainDtoResult = await poolStatInfoGrain.GetAsync();
            var statInfoSnapshot7Day = await GetLast7DaySnapshotListAsync(new StatInfoSnapshot
            {
                StatType = 2,
                Version = dataVersion,
                ChainId = chainId,
                PairAddress = poolStatInfoIndex.PairAddress
            });
            poolStatInfoGrainDtoResult.Data.VolumeInUsd7d = statInfoSnapshot7Day.Sum(t => t.VolumeInUsd);
            var statInfoSnapshot24H = await GetLast24hSnapshotListAsync(new StatInfoSnapshot
            {
                StatType = 2,
                Version = dataVersion,
                ChainId = chainId,
                PairAddress = poolStatInfoIndex.PairAddress
            });
            poolStatInfoGrainDtoResult.Data.VolumeInUsd24h = statInfoSnapshot24H.Sum(t => t.VolumeInUsd);
            poolStatInfoGrainDtoResult.Data.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            poolStatInfoGrainDtoResult = await poolStatInfoGrain.AddOrUpdateAsync(poolStatInfoGrainDtoResult.Data);
            poolStatInfoGrainDtoResult.Data.Version = dataVersion;
            if (poolStatInfoGrainDtoResult.Success)
            {
                await _distributedEventBus.PublishAsync(
                    ObjectMapper.Map<PoolStatInfo, PoolStatInfoEto>(poolStatInfoGrainDtoResult.Data));
            }
        }
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
            tokenStatInfoGrainDtoResult = await tokenStatInfoGrain.AddOrUpdateAsync(tokenStatInfoGrainDtoResult.Data);

            await _distributedEventBus.PublishAsync(ObjectMapper.Map<TokenStatInfo, TokenStatInfoEto>(tokenStatInfoGrainDtoResult.Data));
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

    private async Task<TradePair> GetTradePairAsync(string chainName, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }
    
    private async Task<List<StatInfoSnapshotIndex>> GetLast7DaySnapshotListAsync(StatInfoSnapshot snapshot)
    {
        var period = 3600 * 24;
        var today = DateTime.UtcNow.Date;
        var endTime = DateTimeHelper.ToUnixTimeMilliseconds(today);
        var beginTime = DateTimeHelper.ToUnixTimeMilliseconds(today.AddDays(-6));
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(snapshot.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(snapshot.StatType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(snapshot.Version)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
        if (snapshot.StatType == 1)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(snapshot.Symbol)));
        }
        else if (snapshot.StatType == 2)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(snapshot.PairAddress)));
        }
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(endTime)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).GreaterThanOrEquals(beginTime)));

        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _statInfoSnapshotIndexRepository.GetListAsync(Filter)).Item2;
    }
    
    private async Task<List<StatInfoSnapshotIndex>> GetLast24hSnapshotListAsync(StatInfoSnapshot snapshot)
    {
        var period = 3600;
        var curHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
        var endTime = DateTimeHelper.ToUnixTimeMilliseconds(curHour);
        var beginTime = DateTimeHelper.ToUnixTimeMilliseconds(curHour.AddHours(-23));
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(snapshot.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(snapshot.StatType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(snapshot.Version)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));

        if (snapshot.StatType == 1)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(snapshot.Symbol)));
        }
        else if (snapshot.StatType == 2)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(snapshot.PairAddress)));
        }
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(endTime)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).GreaterThanOrEquals(beginTime)));

        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _statInfoSnapshotIndexRepository.GetListAsync(Filter)).Item2;
    }
    
    private async Task<StatInfoSnapshotIndex> GetLatestSnapshotBefore24hAsync(StatInfoSnapshot snapshot)
    {
        var period = 3600;
        var curHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
        var maxTime = DateTimeHelper.ToUnixTimeMilliseconds(curHour.AddHours(-24));
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(snapshot.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value(snapshot.StatType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(snapshot.Version)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));

        if (snapshot.StatType == 1)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(snapshot.Symbol)));
        }
        else if (snapshot.StatType == 2)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(snapshot.PairAddress)));
        }
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(maxTime)));

        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _statInfoSnapshotIndexRepository.GetSortListAsync(Filter, sortFunc: s => s.Descending(t => t.Timestamp),
            skip: 0, limit: 1);
        return result?.Item2.Count > 0 ? result.Item2[0] : null;
    }
    
    private class PricingNode
    {
        public string FromTokenSymbol { get; set; }
        public string FromTradePairAddress { get; set; }
        public double Tvl { get; set; }
    }
}