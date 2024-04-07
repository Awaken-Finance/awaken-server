using System;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ILiquidityAppService : IApplicationService
    {
        Task InitializeDataAsync();
        
        Task<PagedResultDto<LiquidityRecordIndexDto>> GetRecordsAsync(GetLiquidityRecordsInput input);
        
        Task<PagedResultDto<UserLiquidityIndexDto>> GetUserLiquidityAsync(GetUserLiquidityInput input);

        Task<UserAssetDto> GetUserAssetAsync(GetUserAssertInput input);
        
        Task CreateAsync(LiquidityRecordCreateDto input);
        
        Task CreateAsync(LiquidityRecordDto input);
    }
}