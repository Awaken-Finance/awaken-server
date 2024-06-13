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
    [Route("asset/user-portfolio")]
    public virtual async Task<UserPortfolioDto> UserPortfolioAsync(GetUserPortfolioDto input)
    {
        return await _assetAppService.GetUserPortfolioAsync(input);
    }
    
    [HttpGet]
    [Route("asset/idle-tokens")]
    public virtual async Task<IdleTokensDto> IdleTokensAsync(GetIdleTokensDto input)
    {
        return await _assetAppService.GetIdleTokensAsync(input);
    }
    
    [HttpGet]
    [Route("liquidity/user-positions")]
    public virtual async Task<UserPositionsDto> UserPositionsAsync(GetUserPositionsDto input)
    {
        return await _assetAppService.UserPositionsAsync(input);
    }
    
    [HttpGet]
    [Route("transaction-fee")]
    public virtual async Task<TransactionFeeDto> TransactionFeeAsync()
    {
        return await _assetAppService.GetTransactionFeeAsync();
    }
    
    [HttpPost]
    [Route("user-assets-token")]
    public virtual async Task SetDefaultTokenAsync(SetDefaultTokenDto input)
    {
        await _assetAppService.SetDefaultTokenAsync(input);
    }
    
    
    [HttpGet]
    [Route("user-assets-token")]
    public virtual async Task<DefaultTokenDto> GetDefaultTokenAsync(GetDefaultTokenDto input)
    {
        return await _assetAppService.GetDefaultTokenAsync(input);
    }
}