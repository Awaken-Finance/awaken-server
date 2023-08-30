using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task ReservesAdded_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);

            var benefactor = DebitTestData.Gui;
            var addAmount = "1000";
            var newTotal = "10000";
            await ReservesAddedAsync(CProjectToken.Address, benefactor, addAmount, newTotal, ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);
        }
        
        [Fact(Skip = "no need")]
        public async Task ReservesAdded_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);

            var benefactor = DebitTestData.Gui;
            var addAmount = "1000";
            var newTotal = "10000";
            await ReservesAddedAsync(CProjectToken.Address, benefactor, addAmount, newTotal);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(newTotal);
        }
        
        private async Task ReservesAddedAsync(string cTokenAddress, string benefactor, string addAmount, string newTotalReserves,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var reservesAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<ReservesAdded>>();
            await reservesAddedProcessor.HandleEventAsync(new ReservesAdded
            {
                Benefactor = benefactor,
                AddAmount = BigInteger.Parse(addAmount),
                NewTotalReserves = BigInteger.Parse(newTotalReserves),
            }, GetDefaultEventContext(cTokenAddress, confirmStatus: confirmStatus));
        }
    }
}