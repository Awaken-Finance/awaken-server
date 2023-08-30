using System.Threading.Tasks;
using AwakenServer.Debits.DebitAppDto;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Debits
{
    public interface IDebitAppService
    {
        Task<ListResultDto<CompControllerDto>> GetCompControllerListAsync(GetCompControllerInput input);
        Task<ListResultDto<CTokenDto>> GetCTokenListAsync(GetCTokenListInput input);
        Task<ListResultDto<CTokenUserInfoDto>> GetCTokenUserInfoListAsync(GetCTokenUserInfoInput input);
        Task<PagedResultDto<CTokenRecordDto>> GetCTokenRecordListAsync(GetCTokenRecordInput input);
    }
}