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
    public class NewReserveFactorProcessor : AElfEventProcessorBase<ReserveFactorChanged>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly ILogger<NewReserveFactorProcessor> _logger;

        public NewReserveFactorProcessor(IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService, ILogger<NewReserveFactorProcessor> logger)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(ReserveFactorChanged eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"ReserveFactorChanged Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());

            cToken.ReserveFactorMantissa = eventDetailsEto.NewReserveFactor.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}