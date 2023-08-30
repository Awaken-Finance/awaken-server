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
        public async Task MarketExited_Without_Confirmed_User_Should_Be_Entered()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.First();
            user.IsEnteredMarket.ShouldBe(true);

            await MarketExitedAsync(CProjectToken.Address, DebitTestData.Ming, ContractEventStatus.Unconfirmed);
            (_, users) = await _esUserInfoRepository.GetListAsync();
            user = users.First();
            user.IsEnteredMarket.ShouldBe(true);
        }
        
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
        
        private async Task MarketExitedAsync(string cTokenAddress, string accountAddress,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var marketExitedProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketExited>>();
            await marketExitedProcessor.HandleEventAsync(new MarketExited
            {
                CToken = cTokenAddress,
                Account = accountAddress
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}