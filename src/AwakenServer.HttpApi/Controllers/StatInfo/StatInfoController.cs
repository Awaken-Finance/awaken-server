using System.Threading.Tasks;
using AwakenServer.StatInfo;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.StatInfo
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Info")]
    [Route("/api/app/info")]

    public class StatInfoController : AbpController
    {
        private readonly IStatInfoAppService _statInfoAppService;

        public StatInfoController(IStatInfoAppService statInfoAppService)
        {
            _statInfoAppService = statInfoAppService;
        }

        [HttpGet]
        [Route("tvl/history")]
        public virtual Task<ListResultDto<StatInfoTvlDto>> GetTvlListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTvlListAsync(input);
        }

        [HttpGet]
        [Route("token/tvl/history")]
        public virtual Task<TokenTvlDto> GetTokenTvlListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenTvlListAsync(input);
        }
        
        [HttpGet]
        [Route("pool/tvl/history")]
        public virtual Task<PoolTvlDto> GetPoolTvlListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolTvlListAsync(input);
        }
        
        [HttpGet]
        [Route("volume/history")]
        public virtual Task<ListResultDto<StatInfoVolumeDto>> GetVolumeListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetVolumeListAsync(input);
        }
        
        [HttpGet]
        [Route("token/volume/history")]
        public virtual Task<TokenVolumeDto> GetTokenVolumeListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenVolumeListAsync(input);
        }
        
        [HttpGet]
        [Route("pool/volume/history")]
        public virtual Task<PoolVolumeDto> GetPoolVolumeListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolVolumeListAsync(input);
        }
        
        [HttpGet]
        [Route("token/price/history")]
        public virtual Task<TokenPriceDto> GetTokenPriceListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenPriceListAsync(input);
        }
        
        [HttpGet]
        [Route("pool/price/history")]
        public virtual Task<PoolPriceDto> GetPoolPriceListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolPriceListAsync(input);
        }
        
        [HttpGet]
        [Route("token/list")]
        public virtual Task<ListResultDto<TokenStatInfoDto>> GetTokenStatInfoListAsync(GetTokenStatInfoListInput input)
        {
            return _statInfoAppService.GetTokenStatInfoListAsync(input);
        }
        
        [HttpGet]
        [Route("pool/list")]
        public virtual Task<ListResultDto<PoolStatInfoDto>> GetPoolStatInfoListAsync(GetPoolStatInfoListInput input)
        {
            return _statInfoAppService.GetPoolStatInfoListAsync(input);
        }
        
        [HttpGet]
        [Route("transaction/list")]
        public virtual Task<ListResultDto<TransactionHistoryDto>> GetTransactionStatInfoListAsync(GetTransactionStatInfoListInput input)
        {
            return _statInfoAppService.GetTransactionStatInfoListAsync(input);
        }
    }
}