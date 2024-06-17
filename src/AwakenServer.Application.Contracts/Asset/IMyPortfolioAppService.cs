using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public interface IMyPortfolioAppService
{
    Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input);
    Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input);
}