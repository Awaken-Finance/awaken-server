using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface IKLineAppService : IApplicationService
    {
        Task<ListResultDto<KLineDto>> GetListAsync(GetKLinesInput input);
    }
}