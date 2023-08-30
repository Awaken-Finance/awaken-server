using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.AToken;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.CTokens
{
    public class ReservesAddedProcessor : AElfEventProcessorBase<ReservesAdded>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly ILogger<ReservesAddedProcessor> _logger;

        public ReservesAddedProcessor(IChainAppService chainAppService,
            IRepository<CToken> cTokenRepository, ILogger<ReservesAddedProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(ReservesAdded eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"ReservesAdded Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.TotalUnderlyingAssetReserveAmount = eventDetailsEto.TotalReserves.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}