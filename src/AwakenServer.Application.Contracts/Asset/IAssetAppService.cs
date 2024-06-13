using System.Threading.Tasks;
using AwakenServer.Commons;
using Google.Protobuf.WellKnownTypes;

namespace AwakenServer.Asset;

public interface IAssetAppService
{
    Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input);

    Task<TransactionFeeDto> GetTransactionFeeAsync();

    Task<CommonResponseDto<Empty>> SetDefaultTokenAsync(SetDefaultTokenDto input);

    Task<DefaultTokenDto> GetDefaultTokenAsync(GetDefaultTokenDto input);

    Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input);
    
    Task<IdleTokensDto> GetIdleTokensAsync(GetIdleTokensDto input);
    
    Task<UserPositionsDto> UserPositionsAsync(GetUserPositionsDto input);
    
}