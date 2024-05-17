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

        public Task<TradeRecordIndexDto> GetRecordAsync(string transactionId);

        public Task<TradeRecordIndexDto> GetRecordFromGrainAsync(string chainId, string transactionId);

        Task<PagedResultDto<TradeRecordIndexDto>> GetListAsync(GetTradeRecordsInput input);

        Task CreateAsync(TradeRecordCreateDto input);

        Task<bool> CreateAsync(SwapRecordDto dto);
        
        Task RevertTradeRecordAsync(string chainId);

        Task<int> GetUserTradeAddressCountAsync(string chainId, Guid tradePairId, DateTime? minDateTime = null,
            DateTime? maxDateTime = null);

        Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords);
    }
}