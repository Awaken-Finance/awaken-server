using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class SetPoolProcessor : AElfEventProcessorBase<SetPool>
    {
        private readonly IRepository<AwakenServer.Dividend.Entities.Dividend> _dividendRepository;
        private readonly IRepository<DividendPool> _dividendPoolRepository;
        private readonly IDividendCacheService _dividendCacheService;

        public SetPoolProcessor(IRepository<AwakenServer.Dividend.Entities.Dividend> dividendRepository,
            IRepository<DividendPool> dividendPoolRepository, IDividendCacheService dividendCacheService)
        {
            _dividendRepository = dividendRepository;
            _dividendPoolRepository = dividendPoolRepository;
            _dividendCacheService = dividendCacheService;
        }

        protected override async Task HandleEventAsync(SetPool eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividend =
                await _dividendRepository.GetAsync(x => x.ChainId == chain.Id && x.Address == txInfoDto.EventAddress);
            var dividendPool =
                await _dividendPoolRepository.GetAsync(x =>
                    x.Pid == eventDetailsEto.Pid && x.DividendId == dividend.Id);
            var oldWeight = dividendPool.Weight;
            var newWeight = int.Parse(eventDetailsEto.AllocationPoint.Value);
            dividend.TotalWeight += newWeight - oldWeight;
            await _dividendRepository.UpdateAsync(dividend);
            dividendPool.Weight = newWeight;
            await _dividendPoolRepository.UpdateAsync(dividendPool);
        }
    }
}