using System.Threading.Tasks;
using AwakenServer.Asset;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Asset;

[RemoteService]
[Area("app")]
[ControllerName("Asset")]
[Route("api/app")]
public class AssetController : AbpController
{
    private readonly IAssetAppService _assetAppService;

    public AssetController(IAssetAppService assetAppService)
    {
        _assetAppService = assetAppService;
    }

    [HttpGet]
    [Route("asset/token-list")]
    public virtual async Task<UserAssetInfoDto> TokenListAsync(GetUserAssetInfoDto input)
    {
        return await _assetAppService.GetUserAssetInfoAsync(input);
    }
    
    [HttpGet]
    [Route("transaction-fee")]
    public virtual async Task<TransactionFeeDto> TransactionFeeAsync()
    {
        return await _assetAppService.GetTransactionFeeAsync();
    }
}