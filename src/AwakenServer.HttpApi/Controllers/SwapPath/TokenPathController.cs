using System.Threading.Tasks;
using AwakenServer.SwapTokenPath;
using AwakenServer.SwapTokenPath.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Path 
{
    [RemoteService]
    [Area("app")]
    [ControllerName("SwapPath")]
    [Route("api/app/token-paths")]

    public class TokenPathController : AbpController
    {
        private readonly ITokenPathAppService _tokenPathAppAppService;

        public TokenPathController(ITokenPathAppService tokenPathAppAppService)
        {
            _tokenPathAppAppService = tokenPathAppAppService;
        }

        [HttpGet]
        public virtual Task<PagedResultDto<TokenPathDto>> GetListAsync(GetTokenPathsInput input)
        {
            return _tokenPathAppAppService.GetListAsync(input);
        }
    }
}