using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.AToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{
    public partial class DebitAElfAppServiceTests
    {
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
        
        private async Task ReservesReducedAsync(string cTokenVirtualAddress, Address admin, string reduceAmount, string newTotalReserves)
        {
            var reservesAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<ReservesReduced>>();
            await reservesAddedProcessor.HandleEventAsync(new ReservesReduced
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Sender = admin,
                ReduceAmount = long.Parse(reduceAmount),
                TotalReserves = long.Parse(newTotalReserves)
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr));
        }
    }
}