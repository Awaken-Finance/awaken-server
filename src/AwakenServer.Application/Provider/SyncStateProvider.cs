using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Awaken.Common.HttpClient;
using AwakenServer.ContractEventHandler.Application;
using AwakenServer.Dtos.GraphQL;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.Caching;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;

namespace AwakenServer.Provider;

public class SyncStateProvider : ISyncStateProvider
{
    private readonly HttpProvider _httpProvider;
    private readonly ILogger _logger;
    private readonly IOptionsSnapshot<SyncStateOptions> _syncStateOption;
    private readonly IDistributedCache<BlockChainStatus> _syncStateCache;
    protected const string SyncStatePrefix = "SyncState";
    protected const int SyncStateCacheExpirationTimeSeconds = 1;
    protected const int SyncStateRequestMaxRetries = 3;
    
    public SyncStateProvider(
        HttpProvider httpProvider,
        IOptionsSnapshot<SyncStateOptions> syncStateOption,
        IDistributedCache<BlockChainStatus> syncStateCache)
    {
        _httpProvider = httpProvider;
        _logger = Log.ForContext<SyncStateProvider>();
        _syncStateOption = syncStateOption;
        _syncStateCache = syncStateCache;
    }
    
    public async Task<long> GetLastIrreversibleBlockHeightAsync(string chainId)
    {
        var key = $"{SyncStatePrefix}:{chainId}";
        var chainSyncState = await _syncStateCache.GetAsync(key);
        if (chainSyncState != null)
        {
            _logger.Debug($"Get sync state cache, key: {key}, LastIrreversibleBlockHeight: {chainSyncState.LastIrreversibleBlockHeight}");
            return chainSyncState.LastIrreversibleBlockHeight;
        }
        
        int retryCount = 0;
        while (retryCount < SyncStateRequestMaxRetries)
        {
            var height = await GetLastIrreversibleBlockHeightAsync(chainId, key);
            if (height > 0)
            {
                return height;
            }
            retryCount++;
            _logger.Error($"Attempt {retryCount} failed: GetLastIrreversibleBlockHeightAsync failed.");
            if (retryCount >= SyncStateRequestMaxRetries)
            {
                break;
            }
        }
        _logger.Error($"Get sync state Maximum retry attempts reached, failing with 0.");
        return 0;
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default)]
    public virtual async Task<long> GetLastIrreversibleBlockHeightAsync(string chainId, string key)
    {
        var res = await _httpProvider.InvokeAsync<SyncStateResponse>(HttpMethod.Get, _syncStateOption.Value.Url);
        var syncStateResponse = res.CurrentVersion.Items.FirstOrDefault(i => i.ChainId == chainId);
        if (syncStateResponse != null)
        {
            await _syncStateCache.SetAsync(key, syncStateResponse, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(SyncStateCacheExpirationTimeSeconds)
            });

            _logger.Debug($"Update sync state cache, key: {key}, LastIrreversibleBlockHeight: {syncStateResponse.LastIrreversibleBlockHeight}");
            return syncStateResponse.LastIrreversibleBlockHeight;
        }
        _logger.Error($"Update sync state failed. can't find chainId: {chainId} in response");
        return 0;
    }
}