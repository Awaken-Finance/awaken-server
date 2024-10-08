using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ILiquidityAppService : IApplicationService
    {
        
        Task<PagedResultDto<LiquidityRecordIndexDto>> GetRecordsAsync(GetLiquidityRecordsInput input);
        
        Task<PagedResultDto<UserLiquidityIndexDto>> GetUserLiquidityFromGraphQLAsync(GetUserLiquidityInput input);
        
        Task<UserAssetDto> GetUserAssetFromGraphQLAsync(GetUserAssetInput input);
        
        Task<bool> CreateAsync(LiquidityRecordDto input);

        Task RevertLiquidityAsync(string chainId);

        Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords);
    }
}