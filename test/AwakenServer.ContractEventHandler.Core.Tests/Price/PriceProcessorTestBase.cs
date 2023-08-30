using System;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using Volo.Abp.Threading;

namespace AwakenServer.Price
{
    public class PriceProcessorTestBase : AwakenServerTestBase<PriceProcessorTestModule>
    {
        protected readonly IChainAppService ChainAppService;
        protected string ChainId;

        protected PriceProcessorTestBase()
        {
            ChainAppService = GetRequiredService<IChainAppService>();
            var chainDto = AsyncHelper.RunSync(async () => await ChainAppService.CreateAsync(new ChainCreateDto
            {
                Name = "Ethereum"
            }));
            ChainId = chainDto.Id;
        }
        
        protected ContractEventDetailsDto GetDefaultEventContext(string contractAddress, string txHash = null,
            long timestamp = 0, long blockNumber = 0, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            return new()
            {
                StatusEnum = confirmStatus,
                NodeName = "Ethereum",
                Address = contractAddress,
                TransactionHash = txHash,
                Timestamp = timestamp,
                BlockNumber = blockNumber
            };
        }
    }
}