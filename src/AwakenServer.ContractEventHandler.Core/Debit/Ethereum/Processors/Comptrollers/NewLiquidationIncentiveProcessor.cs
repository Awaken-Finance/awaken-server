// using System.Threading.Tasks;
// using AElf.EthereumNode.EventHandler.Core.DTO;
// using AElf.EthereumNode.EventHandler.Core.ETO;
// using AElf.EthereumNode.EventHandler.Core.Processors;
// using EthereumEventHandler.Providers;
// using AwakenServer.Chains;
// using AwakenServer.Entities.Debits.Ef;
// using Volo.Abp.Domain.Repositories;
//
// namespace AwakenServer.ContractEventHandler.Debit.Processors.Comptrollers
// {
//     public class NewLiquidationIncentiveProcessor : EthereumEthereumEventProcessorBase<NewLiquidationIncentive>
//     {
//         private readonly IRepository<CompController> _compControllerRepository;
//         private readonly ICachedDataProvider<Chain> _chainInfoProvider;
//
//         public NewLiquidationIncentiveProcessor(IRepository<CompController> compControllerRepository,
//             ICachedDataProvider<Chain> chainInfoProvider)
//         {
//             _compControllerRepository = compControllerRepository;
//             _chainAppService = chainAppService;
//         }
//
//         protected override async Task HandleEventAsync(NewLiquidationIncentive eventDetailsEto,
//             ContractEventDetailsDto contractEventDetailsDto)
//         {
//             if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
//             {
//                 return;
//             }
//
//             var nodeName = contractEventDetailsDto.NodeName;
//             var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
//             var compController = await _compControllerRepository.GetAsync(x => x.ChainId == chain.Id);
//             compController.LiquidationIncentive = eventDetailsEto.NewLiquidationIncentiveMantissa.ToString();
//             await _compControllerRepository.UpdateAsync(compController);
//         }
//     }
// }