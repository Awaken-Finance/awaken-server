using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public interface IMyPortfolioAppService
{
    Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto);
    Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto);
}