using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;

namespace AwakenServer.ContractEventHandler.Providers
{
    public interface ICachedDataProvider<T> where T : class, IEntity<Guid>
    {
        Task<T> GetOrSetCachedDataAsync(string key, Expression<Func<T, bool>> predict);
        Task<T> GetOrSetCachedDataByIdAsync(Guid key);
        T GetCachedDataById(Guid key);
        T GetCachedData(string key);
        void SetCachedData(string keyStr, T t, Guid? key = null);
    }

    public abstract class MemoryCachedDataProvider<T> : ICachedDataProvider<T>, ISingletonDependency
        where T : class, IEntity<Guid>
    {
        private readonly ConcurrentDictionary<string, T> _dataCache;
        private readonly ConcurrentDictionary<Guid, T> _dataByIdCache;

        protected MemoryCachedDataProvider()
        {
            _dataCache = new ConcurrentDictionary<string, T>();
            _dataByIdCache = new ConcurrentDictionary<Guid, T>();
        }

        public async Task<T> GetOrSetCachedDataAsync(string key, Expression<Func<T, bool>> predict)
        {
            if (_dataCache.TryGetValue(key, out var cachedData))
            {
                return cachedData;
            }

            cachedData = await GetDataAsync(predict);
            if (cachedData == null)
            {
                return null;
            }

            _dataCache.TryAdd(key, cachedData);
            _dataByIdCache.TryAdd(cachedData.Id, cachedData);
            return cachedData;
        }

        public async Task<T> GetOrSetCachedDataByIdAsync(Guid key)
        {
            if (_dataByIdCache.TryGetValue(key, out var cachedData))
            {
                return cachedData;
            }

            cachedData = await GetDataByIdAsync(key);
            if (cachedData == null)
            {
                return null;
            }
            
            _dataByIdCache.TryAdd(key, cachedData);
            return cachedData;
        }

        public T GetCachedDataById(Guid key)
        {
            if (_dataByIdCache.TryGetValue(key, out var cachedData))
            {
                return cachedData;
            }

            cachedData = AsyncHelper.RunSync(() => GetOrSetCachedDataByIdAsync(key));
            return cachedData;
        }
        
        public T GetCachedData(string key)
        {
            return _dataCache.TryGetValue(key, out var cachedData) ? cachedData : null;
        }

        public void SetCachedData(string keyStr, T t, Guid? key = null)
        {
            _dataCache.TryAdd(keyStr, t);
            if (!key.HasValue)
            {
                return;
            }
            _dataByIdCache.TryAdd(key.Value, t);
        }

        protected abstract Task<T> GetDataAsync(Expression<Func<T, bool>> predict);
        protected abstract Task<T> GetDataByIdAsync(Guid key);
    }
    
    public class DefaultCacheDataProvider<T> : MemoryCachedDataProvider<T> where T : class, IEntity<Guid>
    {
        private readonly IRepository<T> _entityRepository;

        public DefaultCacheDataProvider(IRepository<T> entityRepository)
        {
            _entityRepository = entityRepository;
        }

        protected override async Task<T> GetDataAsync(Expression<Func<T, bool>> predict)
        {
            return await _entityRepository.FindAsync(predict);
        }

        protected override async Task<T> GetDataByIdAsync(Guid key)
        {
            return await _entityRepository.FindAsync(x => (object) x.Id == (object) key);
        }
    }
}