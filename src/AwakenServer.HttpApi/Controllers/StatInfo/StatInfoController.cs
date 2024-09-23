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
        public virtual Task<ListResultDto<StatInfoTvlDto>> GetTvlHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTvlHistoryAsync(input);
        }

        [HttpGet]
        [Route("token/tvl/history")]
        public virtual Task<TokenTvlDto> GetTokenTvlHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenTvlHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("pool/tvl/history")]
        public virtual Task<PoolTvlDto> GetPoolTvlHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolTvlHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("volume/history")]
        public virtual Task<TotalVolumeDto> GetVolumeHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetVolumeHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("token/volume/history")]
        public virtual Task<TokenVolumeDto> GetTokenVolumeHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenVolumeHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("pool/volume/history")]
        public virtual Task<PoolVolumeDto> GetPoolVolumeHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolVolumeHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("token/price/history")]
        public virtual Task<TokenPriceDto> GetTokenPriceHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTokenPriceHistoryAsync(input);
        }
        
        [HttpGet]
        [Route("pool/price/history")]
        public virtual Task<PoolPriceDto> GetPoolPriceHistoryAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetPoolPriceHistoryAsync(input);
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