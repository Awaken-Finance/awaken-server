using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.IDO;
using AwakenServer.IDO.Entities.Ef;
using Awaken.Contracts.Shadowfax;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Processors
{
    public class InvestProcessor : AElfEventProcessorBase<Invest>
    {
        private readonly IRepository<AwakenServer.IDO.Entities.Ef.PublicOffering> _publicOfferingRepository;
        private readonly IRepository<PublicOfferingRecord> _publicOfferingRecordRepository;
        private readonly IRepository<UserPublicOffering> _userOfferingRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<InvestProcessor> _logger;

        public InvestProcessor(IRepository<AwakenServer.IDO.Entities.Ef.PublicOffering> publicOfferingRepository,
            IRepository<PublicOfferingRecord> publicOfferingRecordRepository,
            IRepository<UserPublicOffering> userOfferingRepository, IChainAppService chainAppService,
            ILogger<InvestProcessor> logger)
        {
            _publicOfferingRepository = publicOfferingRepository;
            _publicOfferingRecordRepository = publicOfferingRecordRepository;
            _userOfferingRepository = userOfferingRepository;
            _chainAppService = chainAppService;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(Invest eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _chainAppService.GetByChainIdCacheAsync(txInfoDto.ChainId.ToString());

            var publicOffering = await _publicOfferingRepository.FindAsync(x =>
                x.OrderRank == eventDetailsEto.PublicId && x.ChainId == chain.Id);
            if (publicOffering == null)
            {
                _logger.LogError(eventDetailsEto.ToString());
                throw new Exception($"Failed to find public id {eventDetailsEto.PublicId}");
            }

            publicOffering.RaiseCurrentAmount += eventDetailsEto.Spend;
            publicOffering.CurrentAmount -= eventDetailsEto.Income;
            await _publicOfferingRepository.UpdateAsync(publicOffering);

            var user = eventDetailsEto.Investor.ToBase58();
            var userInfo = await _userOfferingRepository.FindAsync(x =>
                x.User == user && x.PublicOfferingId == publicOffering.Id);
            if (userInfo == null)
            {
                await _userOfferingRepository.InsertAsync(new UserPublicOffering
                {
                    PublicOfferingId = publicOffering.Id,
                    User = user,
                    ChainId = chain.Id,
                    TokenAmount = eventDetailsEto.Income,
                    RaiseTokenAmount = eventDetailsEto.Spend,
                    IsHarvest = false
                });
            }
            else
            {
                userInfo.TokenAmount += eventDetailsEto.Income;
                userInfo.RaiseTokenAmount += eventDetailsEto.Spend;
                await _userOfferingRepository.UpdateAsync(userInfo);
            }

            await _publicOfferingRecordRepository.InsertAsync(new PublicOfferingRecord
            {
                PublicOfferingId = publicOffering.Id,
                User = user,
                OperateType = OperationType.Invest,
                TokenAmount = eventDetailsEto.Income,
                RaiseTokenAmount = eventDetailsEto.Spend,
                DateTime = txInfoDto.BlockTime,
                TransactionHash = txInfoDto.TransactionId,
                ChainId = chain.Id,
                Channel = eventDetailsEto.Channel
            });
        }
    }
}