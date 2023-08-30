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
    public class ReservesReducedProcessor : AElfEventProcessorBase<ReservesReduced>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly ILogger<ReservesReducedProcessor> _logger;

        public ReservesReducedProcessor(IChainAppService chainAppService,
            IRepository<CToken> cTokenRepository, ILogger<ReservesReducedProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(ReservesReduced eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"ReservesReduced Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.TotalUnderlyingAssetReserveAmount = eventDetailsEto.TotalReserves.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}