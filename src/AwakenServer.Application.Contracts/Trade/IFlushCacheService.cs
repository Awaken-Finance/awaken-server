using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade;

public interface IFlushCacheService : IApplicationService
{
    Task FlushCacheAsync(List<string> cacheKeys);
}