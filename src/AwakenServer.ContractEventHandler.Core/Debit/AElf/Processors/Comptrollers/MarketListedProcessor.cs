using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Controller;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.Comptrollers
{
    public class MarketListedProcessor : AElfEventProcessorBase<MarketListed>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<MarketListedProcessor> _logger;

        public MarketListedProcessor(ILogger<MarketListedProcessor> logger, IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService)
        {
            _logger = logger;
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
        }

        protected override async Task HandleEventAsync(MarketListed eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"MarketListed Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var targetCToken =
                await _cTokenRepository.GetAsync(
                    x => x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            targetCToken.IsList = true;
            await _cTokenRepository.UpdateAsync(targetCToken);
        }
    }
}