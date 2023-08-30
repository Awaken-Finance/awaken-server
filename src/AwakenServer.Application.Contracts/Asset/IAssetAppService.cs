using System.Threading.Tasks;

namespace AwakenServer.Asset;

public interface IAssetAppService
{
    Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input);

    Task<TransactionFeeDto> GetTransactionFeeAsync();
}