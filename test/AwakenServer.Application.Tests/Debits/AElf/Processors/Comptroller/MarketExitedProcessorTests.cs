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
        public async Task MarketExited_User_Should_Be_Not_Entered()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            await MarketExitedAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.First();
            user.IsEnteredMarket.ShouldBe(false);
            user.CompInfo.Id.ShouldNotBe(Guid.Empty);
            user.UnderlyingToken.Id.ShouldNotBe(Guid.Empty);
            user.CTokenInfo.Id.ShouldNotBe(Guid.Empty);
        }
        
        private async Task MarketExitedAsync(string cTokenAddress, Address accountAddress)
        {
            var marketExitedProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketExited>>();
            await marketExitedProcessor.HandleEventAsync(new MarketExited
            {
                AToken = Address.FromBase58(cTokenAddress),
                Account = accountAddress
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}