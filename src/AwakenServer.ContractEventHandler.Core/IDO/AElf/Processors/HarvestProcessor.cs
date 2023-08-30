using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.IDO.AElf.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.IDO;
using AwakenServer.IDO.Entities.Ef;
using Awaken.Contracts.Shadowfax;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Processors
{
    public class HarvestProcessor : AElfEventProcessorBase<Harvest>
    {
        private readonly ICachedDataProvider<AwakenServer.IDO.Entities.Ef.PublicOffering> _publicOfferingCache;
        private readonly IRepository<PublicOfferingRecord> _publicOfferingRecordRepository;
        private readonly IRepository<UserPublicOffering> _userOfferingRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<HarvestProcessor> _logger;

        public HarvestProcessor(IChainAppService chainAppService,
            IRepository<UserPublicOffering> userOfferingRepository,
            IRepository<PublicOfferingRecord> publicOfferingRecordRepository,
            ICachedDataProvider<AwakenServer.IDO.Entities.Ef.PublicOffering> publicOfferingCache, ILogger<HarvestProcessor> logger)
        {
            _chainAppService = chainAppService;
            _userOfferingRepository = userOfferingRepository;
            _publicOfferingRecordRepository = publicOfferingRecordRepository;
            _publicOfferingCache = publicOfferingCache;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(Harvest eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _chainAppService.GetByChainIdCacheAsync(txInfoDto.ChainId.ToString());

            var publicOffering = await _publicOfferingCache.GetOrSetCachedDataAsync(
                DataCacheKeyHelper.GetPublicOfferingKey(chain.Id, eventDetailsEto.PublicId), x =>
                    x.OrderRank == eventDetailsEto.PublicId && x.ChainId == chain.Id);
            if (publicOffering == null)
            {
                _logger.LogError(eventDetailsEto.ToString());
                throw new Exception($"Failed to find public id {eventDetailsEto.PublicId}");
            }

            var user = eventDetailsEto.To.ToBase58();
            var userInfo = await _userOfferingRepository.GetAsync(x =>
                x.User == user && x.PublicOfferingId == publicOffering.Id);
            userInfo.IsHarvest = true;
            userInfo.TokenAmount = eventDetailsEto.Amount;
            await _userOfferingRepository.UpdateAsync(userInfo);

            await _publicOfferingRecordRepository.InsertAsync(new PublicOfferingRecord
            {
                PublicOfferingId = publicOffering.Id,
                User = user,
                OperateType = OperationType.Harvest,
                TokenAmount = eventDetailsEto.Amount,
                DateTime = txInfoDto.BlockTime,
                TransactionHash = txInfoDto.TransactionId,
                ChainId = chain.Id
            });
        }
    }
}