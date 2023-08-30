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
        public async Task ReservesReduced_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);

            var admin = DebitTestData.Gui;
            var reduceAmount = "1000";
            var newTotal = "10000";
            await ReservesReducedAsync(CProjectToken.Address, admin, reduceAmount, newTotal, ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);
        }
        
        [Fact(Skip = "no need")]
        public async Task ReservesReduced_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);

            var admin = DebitTestData.Gui;
            var reduceAmount = "1000";
            var newTotal = "10000";
            await ReservesReducedAsync(CProjectToken.Address, admin, reduceAmount, newTotal);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(newTotal);
        }
        
        private async Task ReservesReducedAsync(string cTokenAddress, string admin, string reduceAmount, string newTotalReserves,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var reservesAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<ReservesReduced>>();
            await reservesAddedProcessor.HandleEventAsync(new ReservesReduced
            {
                Admin = admin,
                ReduceAmount = BigInteger.Parse(reduceAmount),
                NewTotalReserves = BigInteger.Parse(newTotalReserves),
            }, GetDefaultEventContext(cTokenAddress, confirmStatus: confirmStatus));
        }
    }
}