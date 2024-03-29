using System;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ITradeRecordAppService : IApplicationService
    {
        Task<PagedResultDto<TradeRecordIndexDto>> GetListAsync(GetTradeRecordsInput input);
        
        Task CreateAsync(TradeRecordCreateDto input);
        
        Task<bool> CreateAsync(SwapRecordDto dto);

        Task CreateCacheAsync(Guid tradePairId, SwapRecordDto dto);

        Task RevertAsync(string chainId);

        Task<int> GetUserTradeAddressCountAsync(string chainId, Guid tradePairId, DateTime? minDateTime = null, DateTime? maxDateTime = null);
    }
}