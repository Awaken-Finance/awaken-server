using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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
        public async Task CompBorrowSpeedUpdate_Without_Confirmed_Should_Not_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var newSpeed = "1234";
            await CompBorrowSpeedUpdateAsync(CProjectToken.Address, newSpeed, ContractEventStatus.Unconfirmed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.BorrowCompSpeed.ShouldNotBe(newSpeed);
        }
        
        [Fact(Skip = "no need")]
        public async Task CompBorrowSpeedUpdate_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var newSpeed = "1234";
            await CompBorrowSpeedUpdateAsync(CProjectToken.Address, newSpeed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.BorrowCompSpeed.ShouldBe(newSpeed);
        }
        
        private async Task CompBorrowSpeedUpdateAsync(string cTokenAddress, string newSpeed,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var compBorrowUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<CompBorrowSpeedUpdated>>();
            await compBorrowUpdateProcessor.HandleEventAsync(new CompBorrowSpeedUpdated
            {
                CToken = cTokenAddress,
                NewSpeed = BigInteger.Parse(newSpeed)
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}