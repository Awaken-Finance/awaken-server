using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.ContractEventHandler.Providers;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class UpToStandardEventProcessor : EthereumEthereumEventProcessorBase<UpToStandardEventDto>
    {
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _gameRepository;
        private readonly IChainAppService _chainAppService;


        public UpToStandardEventProcessor(IRepository<Entities.GameOfTrust.Ef.GameOfTrust> gameRepository,
            IChainAppService chainAppService)
        {
            _gameRepository = gameRepository;
            _chainAppService = chainAppService;
        }

        protected override async Task HandleEventAsync(UpToStandardEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return; 
            }
            
            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var gameOfTrust = await _gameRepository.GetAsync(x => x.Address == contractEventDetailsDto.Address
                                                                  && x.Pid == eventDetailsEto.Pid
                                                                  && x.ChainId == chain.Id);
            gameOfTrust.UnlockHeight = eventDetailsEto.Blocks;
            await _gameRepository.UpdateAsync(gameOfTrust);
        }
    }
}