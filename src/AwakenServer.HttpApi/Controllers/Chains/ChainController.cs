using System;
using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Chains;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Chains
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Chain")]
    [Route("api/app/chains")]
    public class ChainController : AbpController
    {
        private readonly IChainAppService _chainAppService;

        public ChainController(IChainAppService chainAppService)
        {
            _chainAppService = chainAppService;
        }

        [HttpGet]
        public virtual Task<ListResultDto<ChainDto>> GetListAsync()
        {
            return _chainAppService.GetListAsync(new GetChainInput());
        }
        
        [HttpGet]
        [Route("{id}")]
        public virtual Task<ChainDto> GetAsync(string id)
        {
            return _chainAppService.GetChainAsync(id);
        }
        
        [HttpGet]
        [Route("status/{id}")]
        public virtual Task<ChainStatusDto> GetStatusAsync(string id)
        {
            return _chainAppService.GetChainStatusAsync(id);
        }
    }
}