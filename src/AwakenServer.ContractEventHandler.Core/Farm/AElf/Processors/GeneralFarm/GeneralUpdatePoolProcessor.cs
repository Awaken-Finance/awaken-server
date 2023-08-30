using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms.Entities.Ef;
using Awaken.Contracts.PoolTwoContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors.GeneralFarm
{
    public class GeneralUpdatePoolProcessor : AElfEventProcessorBase<UpdatePool>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;

        public GeneralUpdatePoolProcessor(
            IRepository<FarmPool> poolRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _poolRepository = poolRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(UpdatePool eventDetailsEto, EventContext txInfoDto)
        {
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var pool = await _poolRepository.GetAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            pool.AccumulativeDividendProjectToken =
                CalculationHelper.Add(pool.AccumulativeDividendProjectToken, eventDetailsEto.DistributeTokenAmount);
            pool.LastUpdateBlockHeight = pool.LastUpdateBlockHeight < eventDetailsEto.UpdateBlockHeight
                ? eventDetailsEto.UpdateBlockHeight
                : pool.LastUpdateBlockHeight;
            await _poolRepository.UpdateAsync(pool);
        }
    }
}