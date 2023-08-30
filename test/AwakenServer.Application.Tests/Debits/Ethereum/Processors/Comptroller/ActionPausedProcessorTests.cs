using System;
using System.Linq;
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
        public async Task ActionPaused_Without_Confirmed_Should_Not_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.IsMintPaused.ShouldBeFalse();
            cToken.IsBorrowPaused.ShouldBeFalse();
            
            await ActionPausedAsync(CProjectToken.Address, DebitTestData.MintAction, true, ContractEventStatus.Unconfirmed);
            await ActionPausedAsync(CProjectToken.Address, DebitTestData.BorrowAction, true, ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.IsMintPaused.ShouldBeFalse();
            cToken.IsBorrowPaused.ShouldBeFalse();
        }
        
        [Fact(Skip = "no need")]
        public async Task ActionPaused_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await ActionPausedAsync(CProjectToken.Address, DebitTestData.MintAction, true);
            await ActionPausedAsync(CProjectToken.Address, DebitTestData.BorrowAction, true);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.IsMintPaused.ShouldBeTrue();
            cToken.IsBorrowPaused.ShouldBeTrue();
        }
        
        private async Task ActionPausedAsync(string cTokenAddress, string action, bool pausedState,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var actionPausedProcessor = GetRequiredService<IEventHandlerTestProcessor<ActionPaused>>();
            await actionPausedProcessor.HandleEventAsync(new ActionPaused
            {
                CToken = cTokenAddress,
                Action = action,
                PauseState = pausedState
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}