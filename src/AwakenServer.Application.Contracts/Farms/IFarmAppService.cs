using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public interface IFarmAppService
    {
        Task<ListResultDto<FarmDto>> GetFarmListAsync(GetFarmInput input);
        Task<ListResultDto<FarmPoolDto>> GetFarmPoolListAsync(GetFarmPoolInput input);
        Task<ListResultDto<FarmUserInfoDto>> GetFarmUserInfoListAsync(GetFarmUserInfoInput input);
        Task<PagedResultDto<FarmRecordDto>> GetFarmRecordListAsync(GetFarmRecordInput input);
    }
}