using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public interface IMyPortfolioAppService
{
    Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto, string version = "");
    Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto, string version = "");
    Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input);
    Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input);
}