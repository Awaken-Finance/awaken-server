using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ITradeRecordAppService : IApplicationService
    {
        
        Task<PagedResultDto<TradeRecordIndexDto>> GetListAsync(GetTradeRecordsInput input);
        Task<PagedResultDto<TradeRecordIndexDto>> GetListWithSubRecordsAsync(GetTradeRecordsInput input);
        
        Task CreateAsync(TradeRecordCreateDto input);


        Task<bool> CreateAsync(long currentConfirmedHeight, SwapRecordDto dto);

        
        Task RevertTradeRecordAsync(string chainId);

        Task<int> GetUserTradeAddressCountAsync(string chainId, Guid tradePairId, DateTime? minDateTime = null,
            DateTime? maxDateTime = null);

        Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords);
    }
}