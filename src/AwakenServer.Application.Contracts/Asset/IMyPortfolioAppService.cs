using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public interface IMyPortfolioAppService
{
    Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto, bool alignUserAllAsset = true);
    Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto);
    Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input);
    Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input);
    Task<int> UpdateUserAllAssetAsync(string address, TimeSpan maxTimeSinceLastUpdate);
    Task<List<string>> GetAllUserAddressesAsync();
    Task<bool> CleanupUserLiquidityDataAsync(string dataVersion, bool executeDeletion);
    Task<bool> CleanupUserLiquiditySnapshotsDataAsync(string dataVersion, bool executeDeletion);
}