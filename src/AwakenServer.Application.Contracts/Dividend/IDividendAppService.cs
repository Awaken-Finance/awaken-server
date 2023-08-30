using System.Threading.Tasks;
using AwakenServer.Dividend.DividendAppDto;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Dividend
{
    public interface IDividendAppService
    {
        Task<ListResultDto<DividendDto>> GetDividendAsync(GetDividendInput input);
        Task<ListResultDto<DividendPoolDto>> GetDividendPoolsAsync(GetDividendPoolsInput input);
        Task<DividendUserInformationDto> GetUserDividendAsync(GetUserDividendInput input);
        Task<PagedResultDto<DividendUserRecordDto>> GetDividendUserRecordsAsync(GetDividendUserRecordsInput input);
        Task<DividendStatisticDto> GetDividendPoolStatisticAsync(GetDividendPoolStatisticInput input);
        Task<DividendUserStatisticDto> GetUserStatisticAsync(GetUserStatisticInput input);
    }
}