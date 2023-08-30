using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Dividend.Entities;
using AwakenServer.Dividend.Entities.Es;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Services
{
    public interface IDividendCacheService
    {
        Task<Chain> GetCachedChainAsync(int aelfChainId);
        Task<DividendBase> GetDividendBaseInfoAsync(string chainId, string contractAddress);
        Task<AwakenServer.Dividend.Entities.Ef.DividendPool> GetDividendPoolBaseInfoAsync(Guid dividendId, int pid);
        DividendBase GetDividendBaseInfo(Guid dividendId);
        DividendPoolBaseInfo GetDividendPoolBaseInfo(Guid dividendPoolId);
    }

    public class DividendCacheService : IDividendCacheService, ITransientDependency
    {
        private readonly IChainAppService _chainAppService;
        private readonly ICachedDataProvider<AwakenServer.Dividend.Entities.Dividend> _dividendCache;
        private readonly ICachedDataProvider<AwakenServer.Dividend.Entities.Ef.DividendPool> _dividendPoolBaseCache;
        private readonly ConcurrentDictionary<Guid, DividendPoolBaseInfo> _poolBaseDicCache;
        private readonly ITokenAppService _tokenAppService;
        private readonly IObjectMapper _objectMapper;

        public DividendCacheService(IChainAppService chainAppService,
            ICachedDataProvider<AwakenServer.Dividend.Entities.Dividend> dividendCache,
            ICachedDataProvider<AwakenServer.Dividend.Entities.Ef.DividendPool> dividendPoolBaseCache, ITokenAppService tokenAppService,
                IObjectMapper objectMapper)
        {
            _chainAppService = chainAppService;
            _dividendCache = dividendCache;
            _dividendPoolBaseCache = dividendPoolBaseCache;
            _tokenAppService = tokenAppService;
            _poolBaseDicCache = new ConcurrentDictionary<Guid, DividendPoolBaseInfo>();
            _objectMapper = objectMapper;
        }

        public async Task<Chain> GetCachedChainAsync(int aelfChainId)
        {
            return _objectMapper.Map<ChainDto, Chain>(await _chainAppService.GetByChainIdCacheAsync(aelfChainId.ToString()));
        }

        public async Task<DividendBase> GetDividendBaseInfoAsync(string chainId, string contractAddress)
        {
            return await _dividendCache.GetOrSetCachedDataAsync($"{chainId}-{contractAddress}",
                x => x.ChainId == chainId && x.Address == contractAddress);
        }

        public async Task<AwakenServer.Dividend.Entities.Ef.DividendPool> GetDividendPoolBaseInfoAsync(Guid dividendId, int pid)
        {
            return await _dividendPoolBaseCache.GetOrSetCachedDataAsync($"{dividendId}{pid}",
                x => x.DividendId == dividendId && x.Pid == pid);
        }

        public DividendBase GetDividendBaseInfo(Guid dividendId)
        {
            return _dividendCache.GetCachedDataById(dividendId);
        }

        public DividendPoolBaseInfo GetDividendPoolBaseInfo(Guid dividendPoolId)
        {
            if (_poolBaseDicCache.TryGetValue(dividendPoolId, out var poolBaseInfo))
            {
                return poolBaseInfo;
            }

            var pool = _dividendPoolBaseCache.GetCachedDataById(dividendPoolId);
            poolBaseInfo = new DividendPoolBaseInfo
            {
                Id = pool.Id,
                ChainId = pool.ChainId,
                Pid = pool.Pid
            };
            var tokenDto = AsyncHelper.RunSync(async() => await _tokenAppService.GetAsync(pool.PoolTokenId));
            poolBaseInfo.PoolToken = new Token
            {
                Id = tokenDto.Id,
                Decimals = tokenDto.Decimals,
                ChainId = pool.ChainId,
                Symbol = tokenDto.Symbol,
                Address = tokenDto.Address
            };
            poolBaseInfo.Dividend = GetDividendBaseInfo(pool.DividendId);
            _poolBaseDicCache.TryAdd(dividendPoolId, poolBaseInfo);
            return poolBaseInfo;
        }
    }
}