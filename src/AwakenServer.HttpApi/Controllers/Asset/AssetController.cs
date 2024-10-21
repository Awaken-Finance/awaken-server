using System.Globalization;
using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Asset;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Asset;

[RemoteService]
[Area("app")]
[ControllerName("Asset")]
[Route("api/app")]
public class AssetController : AbpController
{
    private readonly IAssetAppService _assetAppService;
    private readonly IMyPortfolioAppService _myPortfolioAppService;
    
    public AssetController(IAssetAppService assetAppService,
        IMyPortfolioAppService myPortfolioAppService)
    {
        _assetAppService = assetAppService;
        _myPortfolioAppService = myPortfolioAppService;
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
        return await _myPortfolioAppService.GetUserPortfolioAsync(input);
    }
    
    [HttpGet]
    [Route("asset/idle-tokens")]
    public virtual async Task<IdleTokensDto> IdleTokensAsync(GetIdleTokensDto input)
    {
        var acceptLanguage = Request.Headers["Accept-Language"].ToString();
        Log.ForContext<AssetController>().Information($"get idle tokens, acceptLanguage: {acceptLanguage}");
        return await _assetAppService.GetIdleTokensAsync(input);
    }
    
    [HttpGet]
    [Route("asset/user-combined-assets")]
    public virtual async Task<UserCombinedAssetsDto> UserCombinedAssetsAsync(GetUserCombinedAssetsDto input)
    {
        return await _assetAppService.GetUserCombinedAssetsAsync(input);
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
    
    [HttpGet]
    [Route("asset/get-user-liquidity")]
    public virtual async Task<CurrentUserLiquidityDto> GetCurrentUserLiquidityAsync(GetCurrentUserLiquidityDto input)
    {
        return await _myPortfolioAppService.GetCurrentUserLiquidityAsync(input);
    }
}