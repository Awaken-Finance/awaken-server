using System.Threading.Tasks;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.StatInfo;

public interface IStatInfoAppService : IApplicationService
{
    Task<ListResultDto<StatInfoTvlDto>> GetTvlListAsync(GetStatHistoryInput input);
    Task<TokenTvlDto> GetTokenTvlListAsync(GetStatHistoryInput input);
    Task<PoolTvlDto> GetPoolTvlListAsync(GetStatHistoryInput input);
    Task<TokenPriceDto> GetTokenPriceListAsync(GetStatHistoryInput input);
    Task<PoolPriceDto> GetPoolPriceListAsync(GetStatHistoryInput input);
    Task<ListResultDto<StatInfoVolumeDto>> GetVolumeListAsync(GetStatHistoryInput input);
    Task<TokenVolumeDto> GetTokenVolumeListAsync(GetStatHistoryInput input);
    Task<PoolVolumeDto> GetPoolVolumeListAsync(GetStatHistoryInput input);
    Task<ListResultDto<TokenStatInfoDto>> GetTokenStatInfoListAsync(GetTokenStatInfoListInput input);
    Task<ListResultDto<PoolStatInfoDto>> GetPoolStatInfoListAsync(GetPoolStatInfoListInput input);
    Task<ListResultDto<TransactionHistoryDto>> GetTransactionStatInfoListAsync(GetTransactionStatInfoListInput input);
}