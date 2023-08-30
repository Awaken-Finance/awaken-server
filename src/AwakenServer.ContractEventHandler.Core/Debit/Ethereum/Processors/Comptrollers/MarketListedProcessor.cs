using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.Comptrollers
{
    public class MarketListedProcessor : EthereumEthereumEventProcessorBase<MarketListed>
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

        protected override async Task HandleEventAsync(MarketListed eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var targetCToken =
                await _cTokenRepository.GetAsync(
                    x => x.ChainId == chain.Id && x.Address == eventDetailsEto.CToken);
            targetCToken.IsList = true;
            await _cTokenRepository.UpdateAsync(targetCToken);
        }
    }
}