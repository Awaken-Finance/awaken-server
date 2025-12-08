using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public interface IMyPortfolioAppService
{
    Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto, string dataVersion, bool alignUserAllAsset = true);
    Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto, string dataVersion);
    Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input);
    Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input);
    Task<int> UpdateUserAllAssetAsync(string address, TimeSpan maxTimeSinceLastUpdate, string dataVersion);
    Task<List<string>> GetAllUserAddressesAsync(string dataVersion);
    Task<bool> CleanupUserLiquidityDataAsync(string dataVersion, bool executeDeletion);
    Task<bool> CleanupUserLiquiditySnapshotsDataAsync(string dataVersion, bool executeDeletion);
    Task<CurrentUserLiquidityDto> GetCurrentUserLiquidityAsync(GetCurrentUserLiquidityDto input);
    List<TokenPortfolioInfoDto> MergeAndProcess(Dictionary<string, TokenPortfolioInfoDto> rawList, int showCount,
        double total);
}