using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ILimitOrderAppService : IApplicationService
    {
        Task<PagedResultDto<LimitOrderIndexDto>> GetListAsync(GetLimitOrdersInput input);
        Task<PagedResultDto<LimitOrderFillRecordIndexDto>> GetListAsync(GetLimitOrderDetailsInput input);
    }
}