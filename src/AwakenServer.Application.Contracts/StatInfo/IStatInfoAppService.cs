using System.Threading.Tasks;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.StatInfo;

public interface IStatInfoAppService : IApplicationService
{
    Task<ListResultDto<StatInfoTvlDto>> GetTvlHistoryAsync(GetStatHistoryInput input);
    Task<TokenTvlDto> GetTokenTvlHistoryAsync(GetStatHistoryInput input);
    Task<PoolTvlDto> GetPoolTvlHistoryAsync(GetStatHistoryInput input);
    Task<TokenPriceDto> GetTokenPriceHistoryAsync(GetStatHistoryInput input);
    Task<PoolPriceDto> GetPoolPriceHistoryAsync(GetStatHistoryInput input);
    Task<TotalVolumeDto> GetVolumeHistoryAsync(GetStatHistoryInput input);
    Task<TokenVolumeDto> GetTokenVolumeHistoryAsync(GetStatHistoryInput input);
    Task<PoolVolumeDto> GetPoolVolumeHistoryAsync(GetStatHistoryInput input);
    Task<ListResultDto<TokenStatInfoDto>> GetTokenStatInfoListAsync(GetTokenStatInfoListInput input);
    Task<ListResultDto<PoolStatInfoDto>> GetPoolStatInfoListAsync(GetPoolStatInfoListInput input);
    Task<ListResultDto<TransactionHistoryDto>> GetTransactionStatInfoListAsync(GetTransactionStatInfoListInput input);
    Task<double> CalculateApr7dAsync(string pairAddress);
}