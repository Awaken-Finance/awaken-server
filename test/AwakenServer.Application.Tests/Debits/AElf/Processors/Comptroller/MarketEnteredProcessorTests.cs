using System;
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
        public async Task MarketEntered_Should_Contain_User()
        {
            var (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(0);
            
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(1);
        }
        
        [Fact(Skip = "no need")]
        public async Task MarketEntered_Repeat_Should_Contain_One_User()
        {
            var (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(0);
            
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(1);
        }

        private async Task MarketEnteredAsync(string cTokenAddress, Address accountAddress)
        {
            var marketEnteredProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketEntered>>();
            await marketEnteredProcessor.HandleEventAsync(new MarketEntered
            {
                AToken = Address.FromBase58(cTokenAddress),
                Account = accountAddress
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}