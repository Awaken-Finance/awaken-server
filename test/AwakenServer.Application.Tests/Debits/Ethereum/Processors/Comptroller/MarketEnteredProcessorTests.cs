using System;
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
        public async Task MarketEntered_Without_Confirmed_Should_Not_Contain_User()
        {
            var (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(0);
            
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming, ContractEventStatus.Unconfirmed);
            (usersCount, _) = await _esUserInfoRepository.GetListAsync();
            usersCount.ShouldBe(0);
        }
        
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

        private async Task MarketEnteredAsync(string cTokenAddress, string accountAddress,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var marketEnteredProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketEntered>>();
            await marketEnteredProcessor.HandleEventAsync(new MarketEntered
            {
                CToken = cTokenAddress,
                Account = accountAddress
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}