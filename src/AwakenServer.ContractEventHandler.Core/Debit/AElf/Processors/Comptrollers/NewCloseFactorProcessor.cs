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
    public class NewCloseFactorProcessor : AElfEventProcessorBase<CloseFactorChanged>
    {
        private readonly IRepository<CompController> _compControllerRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<NewCloseFactorProcessor> _logger;


        public NewCloseFactorProcessor(IRepository<CompController> compControllerRepository,
            IChainAppService chainAppService, ILogger<NewCloseFactorProcessor> logger)
        {
            _compControllerRepository = compControllerRepository;
            _chainAppService = chainAppService;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(CloseFactorChanged eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"CloseFactorChanged Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var compController = await _compControllerRepository.GetAsync(x => x.ChainId == chain.Id);
            compController.CloseFactorMantissa = eventDetailsEto.NewCloseFactor.ToString();
            await _compControllerRepository.UpdateAsync(compController);
        }
    }
}