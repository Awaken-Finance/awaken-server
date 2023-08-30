using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Controller;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{
    public partial class DebitAElfAppServiceTests
    {
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
        
        private async Task ActionPausedAsync(string cTokenAddress, string action, bool pausedState)
        {
            var actionPausedProcessor = GetRequiredService<IEventHandlerTestProcessor<ActionPaused>>();
            await actionPausedProcessor.HandleEventAsync(new ActionPaused
            {
                AToken = Address.FromBase58(cTokenAddress),
                Action = action,
                PauseState = pausedState
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}