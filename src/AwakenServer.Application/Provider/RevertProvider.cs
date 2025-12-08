using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Common;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Trade.Dtos;
using AwakenServer.Worker;
using Microsoft.Extensions.Options;
using Orleans;
using Serilog;

namespace AwakenServer.Provider;

public class RevertProvider : IRevertProvider
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISyncStateProvider _syncStateProvider;
    private readonly TradeRecordRevertWorkerSettings _revertOptions;

    
    public RevertProvider(IClusterClient clusterClient,
        IGraphQLProvider graphQLProvider,
        IOptionsSnapshot<TradeRecordRevertWorkerSettings> tradeRecordOptions,
        ISyncStateProvider syncStateProvider)
    {
        _logger = Log.ForContext<RevertProvider>();
        _clusterClient = clusterClient;
        _graphQlProvider = graphQLProvider;
        _revertOptions = tradeRecordOptions.Value;
        _syncStateProvider = syncStateProvider;
    }
    
    public async Task CheckOrAddUnconfirmedTransaction(long currentConfirmedHeight, EventType eventType, string chainId, long blockHeight,
        string transactionHash)
    {
        var unconfirmedTransactionsGrain = _clusterClient.GetGrain<IUnconfirmedTransactionsGrain>(GrainIdHelper.GenerateGrainId(chainId, eventType));

        if (blockHeight > currentConfirmedHeight)
        {
            await unconfirmedTransactionsGrain.AddAsync(new UnconfirmedTransactionsGrainDto()
            {
                BlockHeight = blockHeight,
                TransactionHash = transactionHash,
            });
        }
    }

    public async Task<List<string>> GetNeedDeleteTransactionsAsync(EventType eventType, string chainId)
    {
        var unconfirmedTransactionsGrain = _clusterClient.GetGrain<IUnconfirmedTransactionsGrain>(GrainIdHelper.GenerateGrainId(chainId, eventType));
        var confirmedHeight = await _syncStateProvider.GetLastIrreversibleBlockHeightAsync(chainId);
        var startBlockHeight = await unconfirmedTransactionsGrain.GetMinUnconfirmedHeightAsync();
        
        var unconfirmedTransactions = await GetUnConfirmedTransactionsAsync(eventType, chainId,
            startBlockHeight, confirmedHeight);
                
        _logger.Information(
            "got unconfirmed transactions, block height range: {0}-{1}, count: {2}, {3}",
            startBlockHeight, confirmedHeight, unconfirmedTransactions.Count(), unconfirmedTransactions.Select(s => s.TransactionHash).ToList());
        
        if (unconfirmedTransactions.Count <= 0)
        {
            return new List<string>();
        }
        
        var confirmedTransactionSet = new HashSet<string>();
        for (int i = 0; i < _revertOptions.RetryLimit; i++)
        {
            var confirmedTransactions = await GetConfirmedTransactionsAsync(eventType, chainId, startBlockHeight, confirmedHeight);
            foreach (var confirmed in confirmedTransactions)
            {
                confirmedTransactionSet.Add(confirmed);
            }
        }
        
        _logger.Information(
            "got confirmed transactions, block height range: {0}-{1}, count: {2}, transaction hash list: {3}",
            startBlockHeight, confirmedHeight, confirmedTransactionSet.Count(),
            confirmedTransactionSet.ToList());
        
        // There may be situations where the confirmed transaction list is empty.
        // if (confirmedTransactionSet.IsNullOrEmpty())
        // {
        //     _logger.Error("confirmed transactions is empty, block height range {0}-{1}", startBlockHeight,
        //         confirmedHeight);
        //     return new List<string>();
        // }
        
        var needDeletedTransactions = unconfirmedTransactions
            .Where(unconfirmed => !confirmedTransactionSet.Contains(unconfirmed.TransactionHash)).ToList();

        _logger.Information(
            "need delete transactions, block height range:{0}-{1}, count:{2}, transaction hash list:{3}",
            startBlockHeight, confirmedHeight, needDeletedTransactions.Count(),
            needDeletedTransactions.Select(s => s.TransactionHash).ToList());
        
        return needDeletedTransactions.Select(dto => dto.TransactionHash).ToList();
    }
    
    public async Task<List<UnconfirmedTransactionsGrainDto>> GetUnConfirmedTransactionsAsync(EventType eventType, string chainId,
        long minUnconfirmedHeight, long confirmedHeight)
    {
        var unconfirmedTransactionsGrain = _clusterClient.GetGrain<IUnconfirmedTransactionsGrain>(GrainIdHelper.GenerateGrainId(chainId, eventType));
        var result = await unconfirmedTransactionsGrain.GetAsync(eventType, minUnconfirmedHeight, confirmedHeight);
        if (result.Success)
        {
            return result.Data;
        }
        else
        {
            _logger.Error($"get unconfirmed transactions failed");
        }

        return new List<UnconfirmedTransactionsGrainDto>();
    }
    
    public async Task<List<string>> GetConfirmedTransactionsAsync(EventType eventType, string chainId, long minUnconfirmedHeight, long confirmedHeight)
    {
        var skipCount = 0;
        var lastEndHeight = minUnconfirmedHeight;
        var page = new List<Tuple<long, string>>();
        var txnHashs = new List<string>();
        do
        {
            switch (eventType)
            {
                case EventType.SwapEvent:
                {
                    var transactions = await _graphQlProvider.GetSwapRecordsAsync(chainId, lastEndHeight, confirmedHeight, skipCount, _revertOptions.QueryOnceLimit);
                    page = transactions.Select(dto => Tuple.Create(dto.BlockHeight, dto.TransactionHash)).ToList();
                    break;
                }
                case EventType.LiquidityEvent:
                {
                    var transactions = await _graphQlProvider.GetLiquidRecordsAsync(chainId, lastEndHeight, confirmedHeight, skipCount, _revertOptions.QueryOnceLimit);
                    page = transactions.Select(dto => Tuple.Create(dto.BlockHeight, dto.TransactionHash)).ToList();
                    break;
                }
                case EventType.TradePairEvent:
                {
                    var transactions = await _graphQlProvider.GetTradePairInfoListAsync(new GetTradePairsInfoInput
                    {
                        ChainId = chainId,
                        StartBlockHeight = lastEndHeight,
                        EndBlockHeight = confirmedHeight,
                        SkipCount = skipCount,
                        MaxResultCount = _revertOptions.QueryOnceLimit
                    });
                    page = transactions.TradePairInfoDtoList.Data.Select(dto => Tuple.Create(dto.BlockHeight, dto.TransactionHash)).ToList();
                    break;
                }
                case EventType.SyncEvent:
                {
                    var transactions = await _graphQlProvider.GetSyncRecordsAsync(chainId, lastEndHeight, confirmedHeight, skipCount, _revertOptions.QueryOnceLimit);
                    page = transactions.Select(dto => Tuple.Create(dto.BlockHeight, dto.TransactionHash)).ToList();
                    break;
                }
            }
            
            if (page.IsNullOrEmpty())
            {
                break;
            }
            
            var maxCurrentBlockHeight = page.Select(x => x.Item1).Max();
            if (maxCurrentBlockHeight == lastEndHeight)
            {
                skipCount += page.Select(x => x.Item1 == lastEndHeight).Count();
            }
            else
            {
                skipCount = page.Select(x => x.Item1 == maxCurrentBlockHeight).Count();
                lastEndHeight = maxCurrentBlockHeight;
            }
            txnHashs.AddRange(page.Select(record => record.Item2));
        } while (!page.IsNullOrEmpty());
        
        return txnHashs;
    }
}