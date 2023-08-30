using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.CoinGeckoApi{

    public interface IRequestLimitProvider
    {
        Task RecordRequestAsync();
    }

    public class RequestLimitProvider : IRequestLimitProvider, ISingletonDependency
    {
        private readonly IDistributedCache<RequestTime> _requestTimeCache;

        public RequestLimitProvider(IDistributedCache<RequestTime> requestTimeCache)
        {
            _requestTimeCache = requestTimeCache;
        }

        public async Task RecordRequestAsync()
        {
            var requestTime = await _requestTimeCache.GetOrAddAsync(CoinGeckoApiConsts.RequestTimeCacheKey,
                async () => new RequestTime(), () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1)
                });
            requestTime.Time += 1;

            if (requestTime.Time > CoinGeckoApiConsts.MaxRequestTime)
            {
                throw new RequestExceedingLimitException("The request exceeded the limit.");
            }

            await _requestTimeCache.SetAsync(CoinGeckoApiConsts.RequestTimeCacheKey, requestTime,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1)
                });
        }
    }

    public class RequestTime
    {
        public int Time { get; set; }
    }
}