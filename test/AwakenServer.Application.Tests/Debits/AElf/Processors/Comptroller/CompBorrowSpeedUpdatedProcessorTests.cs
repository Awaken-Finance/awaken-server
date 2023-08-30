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
        public async Task CompBorrowSpeedUpdate_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var newSpeed = "1234";
            await CompBorrowSpeedUpdateAsync(CProjectToken.Address, newSpeed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.BorrowCompSpeed.ShouldBe(newSpeed);
        }
        
        private async Task CompBorrowSpeedUpdateAsync(string cTokenAddress, string newSpeed)
        {
            var compBorrowUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<PlatformTokenSpeedUpdated>>();
            await compBorrowUpdateProcessor.HandleEventAsync(new PlatformTokenSpeedUpdated
            {
                AToken = Address.FromBase58(cTokenAddress),
                NewSpeed = long.Parse(newSpeed)
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}