using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task MarketListed_Without_Confirmed_Should_Not_Contain_CToken()
        {
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cTokens.Count(x => x.IsList).ShouldBe(0);
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress, ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cTokens.Count(x => x.IsList).ShouldBe(0);
        }
        
        [Fact(Skip = "no need")]
        public async Task MarketListed_Should_Contain_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.IsList.ShouldBeTrue();
        }
        
        private async Task MarketListAsync(string cTokenAddress, string contractAddress, 
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var marketListedProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketListed>>();
            await marketListedProcessor.HandleEventAsync(new MarketListed
            {
                CToken = cTokenAddress
            }, GetDefaultEventContext(contractAddress,confirmStatus: confirmStatus));
        }
        
        private ContractEventDetailsDto GetDefaultEventContext(string contractAddress, string txHash = null,
            long timestamp = 0, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            return new ContractEventDetailsDto
            {
                StatusEnum = confirmStatus,
                NodeName = DebitTestData.DefaultNodeName,
                Address = contractAddress,
                TransactionHash = txHash,
                Timestamp = timestamp
            };
        }
    }
}